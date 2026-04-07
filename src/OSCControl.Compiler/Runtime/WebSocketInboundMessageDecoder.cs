using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace OSCControl.Compiler.Runtime;

internal static class WebSocketInboundMessageDecoder
{
    public static RuntimeEventMessage Decode(string endpointName, WebSocketMessageType messageType, ReadOnlyMemory<byte> payload, string codec)
    {
        return codec switch
        {
            "json" => DecodeJson(endpointName, payload),
            "text" => DecodeText(endpointName, payload),
            "bytes" => DecodeBinary(endpointName, payload),
            _ => throw new NotSupportedException($"Unsupported WebSocket codec '{codec}'.")
        };
    }

    private static RuntimeEventMessage DecodeJson(string endpointName, ReadOnlyMemory<byte> payload)
    {
        using var document = JsonDocument.Parse(payload);
        var root = document.RootElement;

        if (root.ValueKind == JsonValueKind.Object &&
            (root.TryGetProperty("address", out _) ||
             root.TryGetProperty("args", out _) ||
             root.TryGetProperty("body", out _) ||
             root.TryGetProperty("headers", out _) ||
             root.TryGetProperty("extras", out _)))
        {
            var address = root.TryGetProperty("address", out var addressElement) && addressElement.ValueKind != JsonValueKind.Null
                ? addressElement.GetString()
                : null;
            var args = root.TryGetProperty("args", out var argsElement)
                ? RuntimeValueHelpers.NormalizeArgs(RuntimeValueHelpers.FromJsonElement(argsElement))
                : [];
            var body = root.TryGetProperty("body", out var bodyElement)
                ? RuntimeValueHelpers.FromJsonElement(bodyElement)
                : null;
            var headers = root.TryGetProperty("headers", out var headersElement)
                ? RuntimeValueHelpers.NormalizeObject(RuntimeValueHelpers.FromJsonElement(headersElement))
                : new Dictionary<string, object?>(StringComparer.Ordinal);
            var extras = root.TryGetProperty("extras", out var extrasElement)
                ? RuntimeValueHelpers.NormalizeObject(RuntimeValueHelpers.FromJsonElement(extrasElement))
                : new Dictionary<string, object?>(StringComparer.Ordinal);

            return new RuntimeEventMessage(endpointName, address, args, body, headers, extras);
        }

        return new RuntimeEventMessage(endpointName, null, null, RuntimeValueHelpers.FromJsonElement(root));
    }

    private static RuntimeEventMessage DecodeText(string endpointName, ReadOnlyMemory<byte> payload) =>
        new(endpointName, null, null, Encoding.UTF8.GetString(payload.Span));

    private static RuntimeEventMessage DecodeBinary(string endpointName, ReadOnlyMemory<byte> payload) =>
        new(endpointName, null, null, payload.ToArray());
}
