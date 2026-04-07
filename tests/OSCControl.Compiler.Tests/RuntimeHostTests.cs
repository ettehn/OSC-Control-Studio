using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using OSCControl.Compiler.Compiler;
using OSCControl.Compiler.Runtime;
using Xunit;

namespace OSCControl.Compiler.Tests;

public sealed class RuntimeHostTests
{
    [Fact]
    public async Task OscUdpInput_TriggersRuntimeRule()
    {
        var port = GetFreeUdpPort();
        var source = $$"""
endpoint oscIn: osc.udp {
    mode: input
    host: "127.0.0.1"
    port: {{port}}
    codec: osc
}

endpoint oscOut: osc.udp {
    mode: output
    host: "127.0.0.1"
    port: 9001
    codec: osc
}

on receive oscIn when msg.address == "/ping" [
    send oscOut {
        address: "/pong"
        args: [[arg(0)]]
    }
]
""";

        var plan = Assert.IsType<RuntimePlan>(new CompilerPipeline().Compile(source).Plan);
        var transport = new RecordingTransportDispatcher();
        await using var engine = new RuntimeEngine(plan, new RuntimeEngineOptions
        {
            TransportDispatcher = transport
        });
        await using var host = new RuntimeHost(engine);
        await host.StartAsync();

        using var sender = new UdpClient();
        sender.Connect(IPAddress.Loopback, port);
        var packet = OscPacketEncoder.Encode("/ping", [42]);
        await sender.SendAsync(packet, packet.Length);

        await WaitForAsync(() => transport.Records.Count == 1);
        var message = Assert.Single(transport.Records).Message;
        Assert.Equal("/pong", message.Address);
        Assert.Equal([42], message.Args);
    }

    [Fact]
    public async Task WebSocketServerInput_TriggersRuntimeRule()
    {
        var port = GetFreeTcpPort();
        var source = $$"""
endpoint wsIn: ws.server {
    mode: input
    host: "127.0.0.1"
    port: {{port}}
    path: "/control"
    codec: json
}

endpoint oscOut: osc.udp {
    mode: output
    host: "127.0.0.1"
    port: 9001
    codec: osc
}

on receive wsIn when body("action") == "fader" [
    send oscOut {
        address: "/mixer/fader"
        args: [[body("channel"), clamp(body("value"), 0.0, 1.0)]]
    }
]
""";

        var plan = Assert.IsType<RuntimePlan>(new CompilerPipeline().Compile(source).Plan);
        var transport = new RecordingTransportDispatcher();
        var errors = new RecordingRuntimeHostErrorSink();
        await using var engine = new RuntimeEngine(plan, new RuntimeEngineOptions
        {
            TransportDispatcher = transport
        });
        await using var host = new RuntimeHost(engine, new RuntimeHostOptions
        {
            ErrorSink = errors
        });
        try
        {
            await host.StartAsync();
        }
        catch (HttpListenerException ex) when (ex.NativeErrorCode == 6)
        {
            throw new SkipException("HttpListener cannot start in this sandbox: invalid handle.");
        }

        using var socket = new ClientWebSocket();
        await socket.ConnectAsync(new Uri($"ws://127.0.0.1:{port}/control"), CancellationToken.None);
        var payload = Encoding.UTF8.GetBytes("{\"action\":\"fader\",\"channel\":3,\"value\":0.75}");
        await socket.SendAsync(payload, WebSocketMessageType.Text, true, CancellationToken.None);

        await WaitForAsync(() => transport.Records.Count == 1 || errors.Errors.Count > 0);
        if (errors.Errors.FirstOrDefault()?.Exception is HttpListenerException listenerException)
        {
            throw new SkipException($"HttpListener cannot run in this sandbox: {listenerException.Message}");
        }

        Assert.Empty(errors.Errors);

        var message = Assert.Single(transport.Records).Message;
        Assert.Equal("/mixer/fader", message.Address);
        Assert.Equal(2, message.Args.Count);
        Assert.Equal(3d, Convert.ToDouble(message.Args[0]));
        Assert.Equal(0.75d, Assert.IsType<double>(message.Args[1]), 3);
    }

    private static async Task WaitForAsync(Func<bool> predicate, int timeoutMs = 3000)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (DateTime.UtcNow < deadline)
        {
            if (predicate())
            {
                return;
            }

            await Task.Delay(20);
        }

        Assert.True(predicate(), "Condition was not met before timeout.");
    }

    private static int GetFreeUdpPort()
    {
        using var listener = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
        return ((IPEndPoint)listener.Client.LocalEndPoint!).Port;
    }

    private static int GetFreeTcpPort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }
}


