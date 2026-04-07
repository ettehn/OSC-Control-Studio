using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text.Json;

namespace OSCControl.Compiler.Runtime;

internal sealed class DglabSocketSessionRegistry : IAsyncDisposable
{
    private readonly ConcurrentDictionary<string, DglabSocketSession> _sessions = new(StringComparer.Ordinal);

    public DglabSocketSession GetOrCreate(RuntimeResolvedEndpoint endpoint) =>
        _sessions.GetOrAdd(endpoint.Name, _ => new DglabSocketSession(endpoint));

    public async ValueTask RemoveAsync(string endpointName)
    {
        if (_sessions.TryRemove(endpointName, out var session))
        {
            await session.DisposeAsync();
        }
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var session in _sessions.Values)
        {
            await session.DisposeAsync();
        }

        _sessions.Clear();
    }
}

public sealed class DglabSocketSession : IAsyncDisposable
{
    private readonly RuntimeResolvedEndpoint _endpoint;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly List<Func<RuntimeEventMessage, CancellationToken, Task>> _handlers = [];
    private ClientWebSocket? _socket;
    private CancellationTokenSource? _internalCts;
    private Task? _receiveLoop;
    private Task? _heartbeatLoop;
    private bool _disposed;

    public DglabSocketSession(RuntimeResolvedEndpoint endpoint)
    {
        _endpoint = endpoint;
    }

    public string? ClientId { get; private set; }
    public string? TargetId { get; private set; }
    public string? QrUrl { get; private set; }

    public IDisposable Subscribe(Func<RuntimeEventMessage, CancellationToken, Task> handler)
    {
        lock (_handlers)
        {
            _handlers.Add(handler);
        }

        return new Subscription(this, handler);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_receiveLoop is not null)
            {
                return;
            }

            _internalCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _receiveLoop = RunReceiveLoopAsync(_internalCts.Token);
            _heartbeatLoop = RunHeartbeatLoopAsync(_internalCts.Token);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SendCommandAsync(string command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            throw new InvalidOperationException($"DG-LAB send on endpoint '{_endpoint.Name}' requires a non-empty command message.");
        }

        await StartAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(ClientId))
        {
            throw new InvalidOperationException($"DG-LAB endpoint '{_endpoint.Name}' has not received a clientId yet.");
        }

        if (string.IsNullOrWhiteSpace(TargetId))
        {
            throw new InvalidOperationException($"DG-LAB endpoint '{_endpoint.Name}' is not bound to an APP targetId yet. Scan the QR URL first.");
        }

        await SendEnvelopeAsync("msg", ClientId!, TargetId!, command, cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _internalCts?.Cancel();

        if (_socket is not null)
        {
            try
            {
                if (_socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
                {
                    await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "shutdown", CancellationToken.None);
                }
            }
            catch
            {
            }

            _socket.Dispose();
        }

        if (_receiveLoop is not null)
        {
            await SuppressShutdownExceptionsAsync(_receiveLoop);
        }

        if (_heartbeatLoop is not null)
        {
            await SuppressShutdownExceptionsAsync(_heartbeatLoop);
        }

        _internalCts?.Dispose();
        _gate.Dispose();
    }

    private async Task RunReceiveLoopAsync(CancellationToken cancellationToken)
    {
        var buffer = new byte[16 * 1024];

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var socket = await GetOrCreateSocketAsync(cancellationToken);
                while (!cancellationToken.IsCancellationRequested && socket.State == WebSocketState.Open)
                {
                    using var stream = new MemoryStream();
                    WebSocketReceiveResult result;

                    do
                    {
                        result = await socket.ReceiveAsync(buffer, cancellationToken);
                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            await CloseSocketAsync(socket);
                            break;
                        }

                        stream.Write(buffer, 0, result.Count);
                    }
                    while (!result.EndOfMessage);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        break;
                    }

                    await HandleIncomingAsync(stream.ToArray(), cancellationToken);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch
            {
                await ResetSocketAsync();
                await Task.Delay(500, cancellationToken);
            }
        }
    }

    private async Task RunHeartbeatLoopAsync(CancellationToken cancellationToken)
    {
        var interval = GetHeartbeatInterval();
        if (interval <= TimeSpan.Zero)
        {
            return;
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(interval, cancellationToken);
            if (string.IsNullOrWhiteSpace(ClientId))
            {
                continue;
            }

            var targetId = string.IsNullOrWhiteSpace(TargetId) ? ClientId! : TargetId!;
            try
            {
                await SendEnvelopeAsync("heartbeat", ClientId!, targetId, "heartbeat", cancellationToken);
            }
            catch (InvalidOperationException)
            {
            }
            catch (WebSocketException)
            {
                await ResetSocketAsync();
            }
        }
    }

    private async Task<ClientWebSocket> GetOrCreateSocketAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_socket is not null && _socket.State == WebSocketState.Open)
            {
                return _socket;
            }

            _socket?.Dispose();
            _socket = new ClientWebSocket();
            await _socket.ConnectAsync(RuntimeEndpointConfigReader.BuildWebSocketUri(_endpoint), cancellationToken);
            return _socket;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task SendEnvelopeAsync(string type, string clientId, string targetId, string message, CancellationToken cancellationToken)
    {
        var socket = await GetOrCreateSocketAsync(cancellationToken);
        var payload = JsonSerializer.SerializeToUtf8Bytes(new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["type"] = type,
            ["clientId"] = clientId,
            ["targetId"] = targetId,
            ["message"] = message
        });

        if (payload.Length > 1950)
        {
            throw new InvalidOperationException("DG-LAB JSON payload exceeds the 1950-byte protocol limit.");
        }

        await socket.SendAsync(payload, WebSocketMessageType.Text, endOfMessage: true, cancellationToken);
    }

    private async Task HandleIncomingAsync(byte[] payload, CancellationToken cancellationToken)
    {
        var envelope = DglabSocketMessage.Parse(payload);
        if (envelope is null)
        {
            return;
        }

        UpdateBindingState(envelope);
        await PublishAsync(DglabSocketMessage.ToRuntimeEvent(_endpoint.Name, envelope, QrUrl), cancellationToken);
    }

    private void UpdateBindingState(DglabSocketEnvelope envelope)
    {
        if (string.Equals(envelope.Type, "bind", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(envelope.Message, "targetId", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(envelope.ClientId))
        {
            ClientId = envelope.ClientId;
            QrUrl = DglabSocketMessage.BuildQrUrl(_endpoint, envelope.ClientId);
            return;
        }

        if (string.Equals(envelope.Type, "bind", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(envelope.Message, "200", StringComparison.OrdinalIgnoreCase))
        {
            if (string.Equals(envelope.ClientId, ClientId, StringComparison.Ordinal))
            {
                TargetId = envelope.TargetId;
            }
            else if (string.Equals(envelope.TargetId, ClientId, StringComparison.Ordinal))
            {
                TargetId = envelope.ClientId;
            }
            else if (!string.IsNullOrWhiteSpace(envelope.TargetId))
            {
                TargetId = envelope.TargetId;
            }
        }
    }

    private async Task PublishAsync(RuntimeEventMessage message, CancellationToken cancellationToken)
    {
        Func<RuntimeEventMessage, CancellationToken, Task>[] handlers;
        lock (_handlers)
        {
            handlers = _handlers.ToArray();
        }

        foreach (var handler in handlers)
        {
            await handler(message.Clone(), cancellationToken);
        }
    }

    private TimeSpan GetHeartbeatInterval()
    {
        if (_endpoint.Config.TryGetValue("heartbeatIntervalMs", out var raw) && raw is not null)
        {
            var value = Convert.ToInt32(RuntimeValueHelpers.ToNumber(raw));
            return TimeSpan.FromMilliseconds(value);
        }

        return TimeSpan.FromSeconds(30);
    }

    private async Task ResetSocketAsync()
    {
        await _gate.WaitAsync();
        try
        {
            _socket?.Dispose();
            _socket = null;
        }
        finally
        {
            _gate.Release();
        }
    }

    private static async Task CloseSocketAsync(WebSocket socket)
    {
        try
        {
            if (socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
            {
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "closed", CancellationToken.None);
            }
        }
        catch
        {
        }
    }

    private static async Task SuppressShutdownExceptionsAsync(Task task)
    {
        try
        {
            await task;
        }
        catch (OperationCanceledException)
        {
        }
        catch (ObjectDisposedException)
        {
        }
    }

    private void Unsubscribe(Func<RuntimeEventMessage, CancellationToken, Task> handler)
    {
        lock (_handlers)
        {
            _handlers.Remove(handler);
        }
    }

    private sealed class Subscription : IDisposable
    {
        private readonly DglabSocketSession _session;
        private Func<RuntimeEventMessage, CancellationToken, Task>? _handler;

        public Subscription(DglabSocketSession session, Func<RuntimeEventMessage, CancellationToken, Task> handler)
        {
            _session = session;
            _handler = handler;
        }

        public void Dispose()
        {
            var handler = Interlocked.Exchange(ref _handler, null);
            if (handler is not null)
            {
                _session.Unsubscribe(handler);
            }
        }
    }
}

internal sealed record DglabSocketEnvelope(string Type, string ClientId, string TargetId, string Message);

internal static class DglabSocketMessage
{
    public static string CreateCommand(RuntimeOutboundMessage message)
    {
        if (message.Body is string bodyText)
        {
            return bodyText;
        }

        if (message.Body is IReadOnlyDictionary<string, object?> bodyMap &&
            bodyMap.TryGetValue("message", out var bodyMessage) &&
            bodyMessage is not null)
        {
            return RuntimeValueHelpers.ToStringValue(bodyMessage);
        }

        if (!string.IsNullOrWhiteSpace(message.Address))
        {
            return message.Address!;
        }

        throw new InvalidOperationException($"DG-LAB send on endpoint '{message.TargetEndpoint}' requires body: \"strength-...\", body.message, or address.");
    }

    public static DglabSocketEnvelope? Parse(byte[] payload)
    {
        using var document = JsonDocument.Parse(payload);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        return new DglabSocketEnvelope(
            GetString(root, "type"),
            GetString(root, "clientId"),
            GetString(root, "targetId"),
            GetString(root, "message"));
    }

    public static RuntimeEventMessage ToRuntimeEvent(string endpointName, DglabSocketEnvelope envelope, string? qrUrl)
    {
        var body = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["type"] = envelope.Type,
            ["clientId"] = envelope.ClientId,
            ["targetId"] = envelope.TargetId,
            ["message"] = envelope.Message
        };

        var extras = new Dictionary<string, object?>(StringComparer.Ordinal);
        if (!string.IsNullOrWhiteSpace(qrUrl))
        {
            body["qrUrl"] = qrUrl;
            extras["qrUrl"] = qrUrl;
        }

        var address = GetAddress(envelope.Type, envelope.Message);
        var args = GetArgs(envelope.Message);
        return new RuntimeEventMessage(endpointName, address, args, body, extras: extras);
    }

    public static string BuildQrUrl(RuntimeResolvedEndpoint endpoint, string clientId)
    {
        var socketBase = RuntimeEndpointConfigReader.GetOptionalString(endpoint, "qrSocketBaseUrl");
        if (string.IsNullOrWhiteSpace(socketBase))
        {
            var uri = RuntimeEndpointConfigReader.BuildWebSocketUri(endpoint);
            var builder = new UriBuilder(uri.Scheme, uri.Host, uri.IsDefaultPort ? -1 : uri.Port, "/");
            socketBase = builder.Uri.ToString().TrimEnd('/');
        }

        return $"https://www.dungeon-lab.com/app-download.php#DGLAB-SOCKET#{socketBase.TrimEnd('/')}/{clientId}";
    }

    private static string GetString(JsonElement root, string propertyName) =>
        root.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString() ?? string.Empty
            : string.Empty;

    private static string? GetAddress(string type, string message)
    {
        if (string.Equals(type, "bind", StringComparison.OrdinalIgnoreCase))
        {
            return "/dglab/bind";
        }

        if (string.Equals(type, "error", StringComparison.OrdinalIgnoreCase))
        {
            return "/dglab/error";
        }

        if (message.StartsWith("strength-", StringComparison.Ordinal))
        {
            return "/dglab/strength";
        }

        if (message.StartsWith("feedback-", StringComparison.Ordinal))
        {
            return "/dglab/feedback";
        }

        if (message.StartsWith("clear-", StringComparison.Ordinal))
        {
            return "/dglab/clear";
        }

        if (message.StartsWith("pulse-", StringComparison.Ordinal))
        {
            return "/dglab/pulse";
        }

        return "/dglab/" + message.Split('-', 2)[0];
    }

    private static IReadOnlyList<object?> GetArgs(string message)
    {
        if (message.StartsWith("strength-", StringComparison.Ordinal))
        {
            return message["strength-".Length..]
                .Split('+', StringSplitOptions.RemoveEmptyEntries)
                .Select(part => double.TryParse(part, out var value) ? (object?)value : part)
                .ToArray();
        }

        if (message.StartsWith("feedback-", StringComparison.Ordinal) &&
            double.TryParse(message["feedback-".Length..], out var feedback))
        {
            return [feedback];
        }

        return [message];
    }
}
