using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace OSCControl.Compiler.Runtime;

public sealed class NetworkTransportDispatcher : IRuntimeTransportDispatcher, IAsyncDisposable
{
    private readonly Dictionary<string, UdpClient> _udpClients = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ClientWebSocket> _webSockets = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<Guid, WebSocket>> _webSocketServerSockets = new(StringComparer.Ordinal);
    private readonly SemaphoreSlim _gate = new(1, 1);
    private bool _disposed;

    public async Task DispatchAsync(RuntimeResolvedEndpoint endpoint, RuntimeOutboundMessage message, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        switch (endpoint.TransportKind)
        {
            case "osc.udp":
                await DispatchOscAsync(endpoint, message, cancellationToken);
                return;

            case "ws.client":
                await DispatchWebSocketClientAsync(endpoint, message, cancellationToken);
                return;

            case "ws.server":
                await DispatchWebSocketServerAsync(endpoint, message, cancellationToken);
                return;

            default:
                throw new NotSupportedException($"Unsupported transport kind '{endpoint.TransportKind}'.");
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        await _gate.WaitAsync();
        try
        {
            foreach (var webSocket in _webSockets.Values)
            {
                try
                {
                    if (webSocket.State is WebSocketState.Open or WebSocketState.CloseReceived)
                    {
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "shutdown", CancellationToken.None);
                    }
                }
                catch
                {
                }

                webSocket.Dispose();
            }

            foreach (var udpClient in _udpClients.Values)
            {
                udpClient.Dispose();
            }

            _webSockets.Clear();
            _webSocketServerSockets.Clear();
            _udpClients.Clear();
        }
        finally
        {
            _gate.Release();
            _gate.Dispose();
        }
    }

    private async Task DispatchOscAsync(RuntimeResolvedEndpoint endpoint, RuntimeOutboundMessage message, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(message.Address))
        {
            throw new InvalidOperationException($"OSC send on endpoint '{endpoint.Name}' requires an address.");
        }

        var packet = OscPacketEncoder.Encode(message.Address, SelectOscArguments(message));

        var client = await GetOrCreateUdpClientAsync(endpoint, cancellationToken);
        await client.SendAsync(packet, cancellationToken);
    }

    private async Task DispatchWebSocketClientAsync(RuntimeResolvedEndpoint endpoint, RuntimeOutboundMessage message, CancellationToken cancellationToken)
    {
        var socket = await GetOrCreateWebSocketClientAsync(endpoint, cancellationToken);
        var codec = RuntimeEndpointConfigReader.GetOptionalString(endpoint, "codec") ?? "json";
        var payload = CreateWebSocketPayload(message, codec, out var messageType);
        await socket.SendAsync(payload, messageType, endOfMessage: true, cancellationToken);
    }

    private async Task DispatchWebSocketServerAsync(RuntimeResolvedEndpoint endpoint, RuntimeOutboundMessage message, CancellationToken cancellationToken)
    {
        var codec = RuntimeEndpointConfigReader.GetOptionalString(endpoint, "codec") ?? "json";
        var payload = CreateWebSocketPayload(message, codec, out var messageType);
        var sockets = GetOpenWebSocketServerSockets(endpoint.Name);

        foreach (var socket in sockets)
        {
            try
            {
                if (socket.State == WebSocketState.Open)
                {
                    await socket.SendAsync(payload, messageType, endOfMessage: true, cancellationToken);
                }
            }
            catch (WebSocketException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
        }
    }

    private async Task<UdpClient> GetOrCreateUdpClientAsync(RuntimeResolvedEndpoint endpoint, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_udpClients.TryGetValue(endpoint.Name, out var existing))
            {
                return existing;
            }

            var host = RuntimeEndpointConfigReader.GetRequiredString(endpoint, "host");
            var port = RuntimeEndpointConfigReader.GetRequiredInt32(endpoint, "port");
            var client = new UdpClient();
            client.Connect(host, port);
            _udpClients[endpoint.Name] = client;
            return client;
        }
        finally
        {
            _gate.Release();
        }
    }

    internal async Task<ClientWebSocket> GetOrCreateWebSocketClientAsync(RuntimeResolvedEndpoint endpoint, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_webSockets.TryGetValue(endpoint.Name, out var existing) && existing.State == WebSocketState.Open)
            {
                return existing;
            }

            if (_webSockets.TryGetValue(endpoint.Name, out existing))
            {
                existing.Dispose();
                _webSockets.Remove(endpoint.Name);
            }

            var socket = new ClientWebSocket();
            await socket.ConnectAsync(RuntimeEndpointConfigReader.BuildWebSocketUri(endpoint), cancellationToken);
            _webSockets[endpoint.Name] = socket;
            return socket;
        }
        finally
        {
            _gate.Release();
        }
    }

    internal void RegisterWebSocketServerSocket(string endpointName, Guid socketId, WebSocket socket)
    {
        var sockets = _webSocketServerSockets.GetOrAdd(endpointName, static _ => new ConcurrentDictionary<Guid, WebSocket>());
        sockets[socketId] = socket;
    }

    internal void UnregisterWebSocketServerSocket(string endpointName, Guid socketId)
    {
        if (_webSocketServerSockets.TryGetValue(endpointName, out var sockets))
        {
            sockets.TryRemove(socketId, out _);
            if (sockets.IsEmpty)
            {
                _webSocketServerSockets.TryRemove(endpointName, out _);
            }
        }
    }

    internal void RemoveWebSocketClient(string endpointName, ClientWebSocket socket)
    {
        _gate.Wait();
        try
        {
            if (_webSockets.TryGetValue(endpointName, out var existing) && ReferenceEquals(existing, socket))
            {
                _webSockets.Remove(endpointName);
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    private IReadOnlyList<WebSocket> GetOpenWebSocketServerSockets(string endpointName)
    {
        if (!_webSocketServerSockets.TryGetValue(endpointName, out var sockets))
        {
            return [];
        }

        return sockets.Values.Where(static socket => socket.State == WebSocketState.Open).ToArray();
    }

    private static IReadOnlyList<object?> SelectOscArguments(RuntimeOutboundMessage message)
    {
        if (message.Args.Count > 0)
        {
            return message.Args;
        }

        if (message.Body is IReadOnlyList<object?> list)
        {
            return list;
        }

        if (message.Body is List<object?> mutableList)
        {
            return mutableList;
        }

        return message.Body is null ? [] : [message.Body];
    }

    internal static ReadOnlyMemory<byte> CreateWebSocketPayload(RuntimeOutboundMessage message, string codec, out WebSocketMessageType messageType)
    {
        switch (codec)
        {
            case "json":
                messageType = WebSocketMessageType.Text;
                return Encoding.UTF8.GetBytes(JsonSerializer.Serialize(ToWebSocketEnvelope(message)));

            case "text":
                messageType = WebSocketMessageType.Text;
                return Encoding.UTF8.GetBytes(ToTextPayload(message));

            case "bytes":
                messageType = WebSocketMessageType.Binary;
                return ToBinaryPayload(message);

            default:
                throw new NotSupportedException($"Unsupported WebSocket codec '{codec}'.");
        }
    }

    internal static object? ToWebSocketEnvelope(RuntimeOutboundMessage message)
    {
        if (message.Body is not null && string.IsNullOrWhiteSpace(message.Address) && message.Args.Count == 0 && message.Headers.Count == 0 && message.Extras.Count == 0)
        {
            return message.Body;
        }

        var envelope = new Dictionary<string, object?>(StringComparer.Ordinal);
        if (!string.IsNullOrWhiteSpace(message.Address))
        {
            envelope["address"] = message.Address;
        }

        if (message.Args.Count > 0)
        {
            envelope["args"] = message.Args;
        }

        if (message.Body is not null)
        {
            envelope["body"] = message.Body;
        }

        if (message.Headers.Count > 0)
        {
            envelope["headers"] = message.Headers;
        }

        if (message.Extras.Count > 0)
        {
            envelope["extras"] = message.Extras;
        }

        return envelope;
    }

    internal static string ToTextPayload(RuntimeOutboundMessage message)
    {
        if (message.Body is string text && string.IsNullOrWhiteSpace(message.Address) && message.Args.Count == 0 && message.Headers.Count == 0 && message.Extras.Count == 0)
        {
            return text;
        }

        if (message.Body is not null && string.IsNullOrWhiteSpace(message.Address) && message.Args.Count == 0 && message.Headers.Count == 0 && message.Extras.Count == 0)
        {
            return JsonSerializer.Serialize(message.Body);
        }

        return JsonSerializer.Serialize(ToWebSocketEnvelope(message));
    }

    internal static ReadOnlyMemory<byte> ToBinaryPayload(RuntimeOutboundMessage message)
    {
        if (message.Body is byte[] bytes && string.IsNullOrWhiteSpace(message.Address) && message.Args.Count == 0 && message.Headers.Count == 0 && message.Extras.Count == 0)
        {
            return bytes;
        }

        if (message.Args.Count > 0 && message.Args.All(static arg => arg is byte or sbyte or short or ushort or int or uint))
        {
            return message.Args.Select(static arg => Convert.ToByte(arg)).ToArray();
        }

        return Encoding.UTF8.GetBytes(JsonSerializer.Serialize(ToWebSocketEnvelope(message)));
    }
}

internal static class RuntimeEndpointConfigReader
{
    public static string GetRequiredString(RuntimeResolvedEndpoint endpoint, string key)
    {
        if (!endpoint.Config.TryGetValue(key, out var value) || value is null)
        {
            throw new InvalidOperationException($"Endpoint '{endpoint.Name}' is missing required config '{key}'.");
        }

        return RuntimeValueHelpers.ToStringValue(value);
    }

    public static string? GetOptionalString(RuntimeResolvedEndpoint endpoint, string key) =>
        endpoint.Config.TryGetValue(key, out var value) && value is not null
            ? RuntimeValueHelpers.ToStringValue(value)
            : null;

    public static int GetRequiredInt32(RuntimeResolvedEndpoint endpoint, string key)
    {
        if (!endpoint.Config.TryGetValue(key, out var value) || value is null)
        {
            throw new InvalidOperationException($"Endpoint '{endpoint.Name}' is missing required config '{key}'.");
        }

        return Convert.ToInt32(RuntimeValueHelpers.ToNumber(value));
    }

    public static Uri BuildWebSocketUri(RuntimeResolvedEndpoint endpoint)
    {
        var host = GetRequiredString(endpoint, "host");
        var port = GetRequiredInt32(endpoint, "port");
        var path = NormalizeHttpPath(GetOptionalString(endpoint, "path") ?? "/");
        var secure = string.Equals(GetOptionalString(endpoint, "secure"), "true", StringComparison.OrdinalIgnoreCase);

        return new UriBuilder(secure ? "wss" : "ws", host, port, path).Uri;
    }

    public static IPAddress ResolveBindAddress(string host)
    {
        if (string.Equals(host, "0.0.0.0", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(host, "*", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(host, "+", StringComparison.OrdinalIgnoreCase))
        {
            return IPAddress.Any;
        }

        if (string.Equals(host, "::", StringComparison.OrdinalIgnoreCase))
        {
            return IPAddress.IPv6Any;
        }

        if (string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase))
        {
            return IPAddress.Loopback;
        }

        if (IPAddress.TryParse(host, out var address))
        {
            return address;
        }

        return Dns.GetHostAddresses(host).First();
    }

    public static string BuildHttpListenerPrefix(RuntimeResolvedEndpoint endpoint)
    {
        var host = GetRequiredString(endpoint, "host");
        var port = GetRequiredInt32(endpoint, "port");
        var path = NormalizeHttpPath(GetOptionalString(endpoint, "path") ?? "/");
        var secure = string.Equals(GetOptionalString(endpoint, "secure"), "true", StringComparison.OrdinalIgnoreCase);
        var httpHost = host switch
        {
            "0.0.0.0" => "+",
            "*" => "+",
            _ => host
        };

        return $"{(secure ? "https" : "http")}://{httpHost}:{port}{EnsureTrailingSlash(path)}";
    }

    private static string NormalizeHttpPath(string path)
    {
        if (!path.StartsWith("/", StringComparison.Ordinal))
        {
            path = "/" + path;
        }

        return path;
    }

    private static string EnsureTrailingSlash(string path) =>
        path.EndsWith("/", StringComparison.Ordinal) ? path : path + "/";
}
