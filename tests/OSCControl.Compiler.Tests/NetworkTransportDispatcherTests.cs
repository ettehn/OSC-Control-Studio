using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using OSCControl.Compiler.Runtime;
using Xunit;

namespace OSCControl.Compiler.Tests;

public sealed class NetworkTransportDispatcherTests
{
    [Fact]
    public async Task DispatchAsync_OscUdp_SendsOscPacket()
    {
        using var listener = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
        var port = ((IPEndPoint)listener.Client.LocalEndPoint!).Port;
        await using var dispatcher = new NetworkTransportDispatcher();

        var endpoint = new RuntimeResolvedEndpoint(
            "oscOut",
            "osc.udp",
            new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["host"] = "127.0.0.1",
                ["port"] = port,
                ["codec"] = "osc"
            });

        var receiveTask = listener.ReceiveAsync();
        await dispatcher.DispatchAsync(endpoint, new RuntimeOutboundMessage(
            "oscOut",
            "/note/on",
            [60, 0.5d, "hello", true, null],
            null,
            new Dictionary<string, object?>(),
            new Dictionary<string, object?>()),
            CancellationToken.None);

        var received = await receiveTask;
        var decoded = DecodeOscArguments(received.Buffer);

        Assert.Equal("/note/on", decoded.Address);
        Assert.Equal(new[] { 'i', 'd', 's', 'T', 'N' }, decoded.TypeTags);
        Assert.Equal(60, decoded.Values[0]);
        Assert.Equal(0.5d, Assert.IsType<double>(decoded.Values[1]), 3);
        Assert.Equal("hello", decoded.Values[2]);
    }

    [Fact]
    public void CreateWebSocketPayload_Json_EncodesEnvelope()
    {
        var message = new RuntimeOutboundMessage(
            "wsOut",
            "/chatbox/input",
            ["Hello", true],
            new Dictionary<string, object?> { ["kind"] = "chat" },
            new Dictionary<string, object?>(),
            new Dictionary<string, object?> { ["notify"] = false });

        var payload = NetworkTransportDispatcher.CreateWebSocketPayload(message, "json", out var messageType);
        var json = JsonSerializer.Deserialize<JsonElement>(payload.Span);

        Assert.Equal(WebSocketMessageType.Text, messageType);
        Assert.Equal("/chatbox/input", json.GetProperty("address").GetString());
        Assert.Equal("Hello", json.GetProperty("args")[0].GetString());
        Assert.True(json.GetProperty("args")[1].GetBoolean());
        Assert.Equal("chat", json.GetProperty("body").GetProperty("kind").GetString());
        Assert.False(json.GetProperty("extras").GetProperty("notify").GetBoolean());
    }

    [Fact]
    public void DglabSocketMessage_MapsBindAndCommand()
    {
        var endpoint = new RuntimeResolvedEndpoint(
            "dglab",
            "dglab.socket",
            new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["host"] = "127.0.0.1",
                ["port"] = 5678,
                ["path"] = "/socket",
                ["qrSocketBaseUrl"] = "wss://example.test"
            });

        var qrUrl = DglabSocketMessage.BuildQrUrl(endpoint, "terminal-id");
        Assert.Equal("https://www.dungeon-lab.com/app-download.php#DGLAB-SOCKET#wss://example.test/terminal-id", qrUrl);

        var message = DglabSocketMessage.ToRuntimeEvent("dglab", new DglabSocketEnvelope("bind", "terminal-id", "app-id", "200"), qrUrl);
        Assert.Equal("/dglab/bind", message.Address);
        Assert.Equal("200", Assert.IsType<Dictionary<string, object?>>(message.Body)["message"]);
        Assert.Equal(qrUrl, message.Extras["qrUrl"]);

        var command = DglabSocketMessage.CreateCommand(new RuntimeOutboundMessage(
            "dglab",
            null,
            [],
            "strength-1+2+50",
            new Dictionary<string, object?>(),
            new Dictionary<string, object?>()));
        Assert.Equal("strength-1+2+50", command);
    }

    private static (string Address, char[] TypeTags, object?[] Values) DecodeOscArguments(byte[] packet)
    {
        var offset = 0;
        var address = ReadOscString(packet, ref offset);
        var typeTagText = ReadOscString(packet, ref offset);
        var values = new List<object?>();

        foreach (var typeTag in typeTagText.Skip(1))
        {
            values.Add(typeTag switch
            {
                'i' => ReadInt32(packet, ref offset),
                'h' => ReadInt64(packet, ref offset),
                'f' => ReadSingle(packet, ref offset),
                'd' => ReadDouble(packet, ref offset),
                's' => ReadOscString(packet, ref offset),
                'T' => true,
                'F' => false,
                'N' => null,
                _ => throw new InvalidOperationException($"Unsupported OSC type tag '{typeTag}'.")
            });
        }

        return (address, typeTagText.Skip(1).ToArray(), values.ToArray());
    }

    private static string ReadOscString(byte[] packet, ref int offset)
    {
        var start = offset;
        while (packet[offset] != 0)
        {
            offset++;
        }

        var value = Encoding.UTF8.GetString(packet, start, offset - start);
        offset++;
        while (offset % 4 != 0)
        {
            offset++;
        }

        return value;
    }

    private static int ReadInt32(byte[] packet, ref int offset)
    {
        var value = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(packet, offset));
        offset += 4;
        return value;
    }

    private static long ReadInt64(byte[] packet, ref int offset)
    {
        var value = IPAddress.NetworkToHostOrder(BitConverter.ToInt64(packet, offset));
        offset += 8;
        return value;
    }

    private static float ReadSingle(byte[] packet, ref int offset)
    {
        var bits = ReadInt32(packet, ref offset);
        return BitConverter.Int32BitsToSingle(bits);
    }

    private static double ReadDouble(byte[] packet, ref int offset)
    {
        var bits = ReadInt64(packet, ref offset);
        return BitConverter.Int64BitsToDouble(bits);
    }
}
