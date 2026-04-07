using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;

namespace OSCControl.Compiler.Runtime;

public sealed class RuntimeHost : IAsyncDisposable
{
    private readonly RuntimeEngine _engine;
    private readonly IRuntimeHostErrorSink _errorSink;
    private readonly IReadOnlyDictionary<string, RuntimeResolvedEndpoint> _endpoints;
    private readonly List<IRuntimeInboundAdapter> _adapters;
    private readonly NetworkTransportDispatcher? _networkTransportDispatcher;
    private CancellationTokenSource? _lifetimeCts;
    private bool _started;
    private bool _disposed;

    public RuntimeHost(RuntimeEngine engine, RuntimeHostOptions? options = null)
    {
        _engine = engine;
        options ??= new RuntimeHostOptions();
        _errorSink = options.ErrorSink ?? new RecordingRuntimeHostErrorSink();
        _endpoints = RuntimeEndpointResolver.Resolve(engine.Endpoints);
        _networkTransportDispatcher = _engine.TransportDispatcher as NetworkTransportDispatcher;
        _adapters = CreateAdapters(_endpoints.Values).ToList();
    }

    public RuntimeEngine Engine => _engine;
    public IRuntimeHostErrorSink ErrorSink => _errorSink;

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_started)
        {
            return;
        }

        _lifetimeCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        foreach (var adapter in _adapters)
        {
            await adapter.StartAsync(_lifetimeCts.Token);
        }

        _started = true;
        await _engine.StartAsync(_lifetimeCts.Token);
    }

    public async Task StopAsync()
    {
        if (!_started)
        {
            return;
        }

        _lifetimeCts?.Cancel();
        foreach (var adapter in _adapters)
        {
            await adapter.DisposeAsync();
        }

        _lifetimeCts?.Dispose();
        _lifetimeCts = null;
        _started = false;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        await StopAsync();
    }

    private IEnumerable<IRuntimeInboundAdapter> CreateAdapters(IEnumerable<RuntimeResolvedEndpoint> endpoints)
    {
        foreach (var endpoint in endpoints)
        {
            var mode = RuntimeEndpointConfigReader.GetOptionalString(endpoint, "mode") ?? "input";
            if (!string.Equals(mode, "input", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(mode, "duplex", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            switch (endpoint.TransportKind)
            {
                case "osc.udp":
                    yield return new OscUdpInboundAdapter(endpoint, HandleInboundAsync, _errorSink);
                    break;
                case "ws.client":
                    if (_networkTransportDispatcher is not null)
                    {
                        yield return new WebSocketClientInboundAdapter(endpoint, _networkTransportDispatcher, HandleInboundAsync, _errorSink);
                    }
                    break;
                case "ws.server":
                    yield return new WebSocketServerInboundAdapter(endpoint, HandleInboundAsync, _errorSink, _networkTransportDispatcher);
                    break;
            }
        }
    }

    private Task HandleInboundAsync(string endpointName, RuntimeEventMessage message, CancellationToken cancellationToken) =>
        _engine.ReceiveAsync(endpointName, message, cancellationToken);
}

internal interface IRuntimeInboundAdapter : IAsyncDisposable
{
    Task StartAsync(CancellationToken cancellationToken);
}

internal sealed class OscUdpInboundAdapter : IRuntimeInboundAdapter
{
    private readonly RuntimeResolvedEndpoint _endpoint;
    private readonly Func<string, RuntimeEventMessage, CancellationToken, Task> _onMessage;
    private readonly IRuntimeHostErrorSink _errorSink;
    private UdpClient? _udpClient;
    private Task? _receiveLoop;
    private CancellationTokenSource? _internalCts;

    public OscUdpInboundAdapter(
        RuntimeResolvedEndpoint endpoint,
        Func<string, RuntimeEventMessage, CancellationToken, Task> onMessage,
        IRuntimeHostErrorSink errorSink)
    {
        _endpoint = endpoint;
        _onMessage = onMessage;
        _errorSink = errorSink;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (_receiveLoop is not null)
        {
            return Task.CompletedTask;
        }

        _internalCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var host = RuntimeEndpointConfigReader.GetOptionalString(_endpoint, "host") ?? "0.0.0.0";
        var port = RuntimeEndpointConfigReader.GetRequiredInt32(_endpoint, "port");
        _udpClient = new UdpClient(new IPEndPoint(RuntimeEndpointConfigReader.ResolveBindAddress(host), port));
        _receiveLoop = RunReceiveLoopAsync(_internalCts.Token);
        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        _internalCts?.Cancel();
        _udpClient?.Dispose();

        if (_receiveLoop is not null)
        {
            try
            {
                await _receiveLoop;
            }
            catch (OperationCanceledException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
        }

        _internalCts?.Dispose();
    }

    private async Task RunReceiveLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var result = await _udpClient!.ReceiveAsync(cancellationToken);
                var message = OscPacketDecoder.Decode(
                    _endpoint.Name,
                    result.Buffer,
                    result.RemoteEndPoint.Address.ToString(),
                    result.RemoteEndPoint.Port);
                await _onMessage(_endpoint.Name, message, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _errorSink.Report(new RuntimeHostError(_endpoint.Name, _endpoint.TransportKind, "receive", ex, DateTimeOffset.UtcNow));
            }
        }
    }
}

internal sealed class WebSocketClientInboundAdapter : IRuntimeInboundAdapter
{
    private readonly RuntimeResolvedEndpoint _endpoint;
    private readonly NetworkTransportDispatcher _dispatcher;
    private readonly Func<string, RuntimeEventMessage, CancellationToken, Task> _onMessage;
    private readonly IRuntimeHostErrorSink _errorSink;
    private Task? _receiveLoop;
    private CancellationTokenSource? _internalCts;

    public WebSocketClientInboundAdapter(
        RuntimeResolvedEndpoint endpoint,
        NetworkTransportDispatcher dispatcher,
        Func<string, RuntimeEventMessage, CancellationToken, Task> onMessage,
        IRuntimeHostErrorSink errorSink)
    {
        _endpoint = endpoint;
        _dispatcher = dispatcher;
        _onMessage = onMessage;
        _errorSink = errorSink;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (_receiveLoop is not null)
        {
            return Task.CompletedTask;
        }

        _internalCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _receiveLoop = RunReceiveLoopAsync(_internalCts.Token);
        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        _internalCts?.Cancel();

        if (_receiveLoop is not null)
        {
            try
            {
                await _receiveLoop;
            }
            catch (OperationCanceledException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
        }

        _internalCts?.Dispose();
    }

    private async Task RunReceiveLoopAsync(CancellationToken cancellationToken)
    {
        var codec = RuntimeEndpointConfigReader.GetOptionalString(_endpoint, "codec") ?? "json";
        var buffer = new byte[16 * 1024];

        while (!cancellationToken.IsCancellationRequested)
        {
            ClientWebSocket? socket = null;
            try
            {
                socket = await _dispatcher.GetOrCreateWebSocketClientAsync(_endpoint, cancellationToken);
                await ReceiveMessagesAsync(socket, codec, buffer, cancellationToken);
                _dispatcher.RemoveWebSocketClient(_endpoint.Name, socket);
                socket.Dispose();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                if (socket is not null)
                {
                    _dispatcher.RemoveWebSocketClient(_endpoint.Name, socket);
                    socket.Dispose();
                }

                _errorSink.Report(new RuntimeHostError(_endpoint.Name, _endpoint.TransportKind, "client", ex, DateTimeOffset.UtcNow));
                try
                {
                    await Task.Delay(500, cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }
    }

    private async Task ReceiveMessagesAsync(ClientWebSocket socket, string codec, byte[] buffer, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && socket.State == WebSocketState.Open)
        {
            using var stream = new MemoryStream();
            WebSocketReceiveResult result;

            do
            {
                result = await socket.ReceiveAsync(buffer, cancellationToken);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    if (socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
                    {
                        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "closed", CancellationToken.None);
                    }

                    return;
                }

                stream.Write(buffer, 0, result.Count);
            }
            while (!result.EndOfMessage);

            var message = WebSocketInboundMessageDecoder.Decode(_endpoint.Name, result.MessageType, stream.ToArray(), codec);
            await _onMessage(_endpoint.Name, message, cancellationToken);
        }
    }
}

internal sealed class WebSocketServerInboundAdapter : IRuntimeInboundAdapter
{
    private readonly RuntimeResolvedEndpoint _endpoint;
    private readonly Func<string, RuntimeEventMessage, CancellationToken, Task> _onMessage;
    private readonly IRuntimeHostErrorSink _errorSink;
    private readonly NetworkTransportDispatcher? _dispatcher;
    private readonly ConcurrentDictionary<Guid, WebSocket> _sockets = new();
    private readonly ConcurrentBag<Task> _connectionTasks = [];
    private HttpListener? _listener;
    private Task? _acceptLoop;
    private CancellationTokenSource? _internalCts;

    public WebSocketServerInboundAdapter(
        RuntimeResolvedEndpoint endpoint,
        Func<string, RuntimeEventMessage, CancellationToken, Task> onMessage,
        IRuntimeHostErrorSink errorSink,
        NetworkTransportDispatcher? dispatcher)
    {
        _endpoint = endpoint;
        _onMessage = onMessage;
        _errorSink = errorSink;
        _dispatcher = dispatcher;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (_acceptLoop is not null)
        {
            return Task.CompletedTask;
        }

        _internalCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _listener = new HttpListener();
        _listener.Prefixes.Add(RuntimeEndpointConfigReader.BuildHttpListenerPrefix(_endpoint));
        _listener.Start();
        _acceptLoop = RunAcceptLoopAsync(_internalCts.Token);
        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        _internalCts?.Cancel();

        if (_listener is not null)
        {
            try
            {
                _listener.Stop();
            }
            catch
            {
            }

            _listener.Close();
        }

        foreach (var socket in _sockets.Values)
        {
            try
            {
                if (socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
                {
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "shutdown", CancellationToken.None);
                }
            }
            catch
            {
            }

            socket.Dispose();
        }

        if (_acceptLoop is not null)
        {
            try
            {
                await _acceptLoop;
            }
            catch (OperationCanceledException)
            {
            }
            catch (HttpListenerException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
        }

        await Task.WhenAll(_connectionTasks.ToArray());
        _internalCts?.Dispose();
    }

    private async Task RunAcceptLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var context = await _listener!.GetContextAsync().WaitAsync(cancellationToken);
                if (!context.Request.IsWebSocketRequest)
                {
                    context.Response.StatusCode = 400;
                    context.Response.Close();
                    continue;
                }

                var webSocketContext = await context.AcceptWebSocketAsync(null);
                var socketId = Guid.NewGuid();
                _sockets[socketId] = webSocketContext.WebSocket;
                _dispatcher?.RegisterWebSocketServerSocket(_endpoint.Name, socketId, webSocketContext.WebSocket);
                var task = HandleConnectionAsync(socketId, webSocketContext.WebSocket, cancellationToken);
                _connectionTasks.Add(task);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (HttpListenerException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _errorSink.Report(new RuntimeHostError(_endpoint.Name, _endpoint.TransportKind, "accept", ex, DateTimeOffset.UtcNow));
            }
        }
    }

    private async Task HandleConnectionAsync(Guid socketId, WebSocket socket, CancellationToken cancellationToken)
    {
        var codec = RuntimeEndpointConfigReader.GetOptionalString(_endpoint, "codec") ?? "json";
        var buffer = new byte[16 * 1024];

        try
        {
            while (!cancellationToken.IsCancellationRequested && socket.State == WebSocketState.Open)
            {
                using var stream = new MemoryStream();
                WebSocketReceiveResult result;

                do
                {
                    result = await socket.ReceiveAsync(buffer, cancellationToken);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "closed", CancellationToken.None);
                        return;
                    }

                    stream.Write(buffer, 0, result.Count);
                }
                while (!result.EndOfMessage);

                var message = WebSocketInboundMessageDecoder.Decode(_endpoint.Name, result.MessageType, stream.ToArray(), codec);
                message.Extras["remoteAddress"] = socketId.ToString("N");
                await _onMessage(_endpoint.Name, message, cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (WebSocketException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            _errorSink.Report(new RuntimeHostError(_endpoint.Name, _endpoint.TransportKind, "connection", ex, DateTimeOffset.UtcNow));
        }
        finally
        {
            _dispatcher?.UnregisterWebSocketServerSocket(_endpoint.Name, socketId);
            _sockets.TryRemove(socketId, out _);
            socket.Dispose();
        }
    }
}
