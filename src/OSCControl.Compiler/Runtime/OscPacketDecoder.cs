using System.Buffers.Binary;

namespace OSCControl.Compiler.Runtime;

internal static class OscPacketDecoder
{
    public static RuntimeEventMessage Decode(string endpointName, byte[] packet, string? remoteAddress = null, int? remotePort = null)
    {
        var offset = 0;
        var address = ReadPaddedString(packet, ref offset);
        var typeTagText = ReadPaddedString(packet, ref offset);
        if (string.IsNullOrEmpty(typeTagText) || typeTagText[0] != ',')
        {
            throw new InvalidOperationException("OSC packet is missing a valid type tag string.");
        }

        var args = new List<object?>();
        foreach (var tag in typeTagText.Skip(1))
        {
            args.Add(ReadArgument(packet, ref offset, tag));
        }

        var extras = new Dictionary<string, object?>(StringComparer.Ordinal);
        if (!string.IsNullOrWhiteSpace(remoteAddress))
        {
            extras["remoteAddress"] = remoteAddress;
        }

        if (remotePort is not null)
        {
            extras["remotePort"] = remotePort.Value;
        }

        return new RuntimeEventMessage(endpointName, address, args, null, null, extras);
    }

    private static object? ReadArgument(byte[] packet, ref int offset, char tag) => tag switch
    {
        'i' => ReadInt32(packet, ref offset),
        'h' => ReadInt64(packet, ref offset),
        'f' => ReadSingle(packet, ref offset),
        'd' => ReadDouble(packet, ref offset),
        's' => ReadPaddedString(packet, ref offset),
        'b' => ReadBlob(packet, ref offset),
        'T' => true,
        'F' => false,
        'N' => null,
        _ => throw new NotSupportedException($"Unsupported OSC type tag '{tag}'.")
    };

    private static string ReadPaddedString(byte[] packet, ref int offset)
    {
        var start = offset;
        while (offset < packet.Length && packet[offset] != 0)
        {
            offset++;
        }

        var value = System.Text.Encoding.UTF8.GetString(packet, start, offset - start);
        offset++;
        while (offset % 4 != 0)
        {
            offset++;
        }

        return value;
    }

    private static byte[] ReadBlob(byte[] packet, ref int offset)
    {
        var length = ReadInt32(packet, ref offset);
        var blob = new byte[length];
        Buffer.BlockCopy(packet, offset, blob, 0, length);
        offset += length;
        while (offset % 4 != 0)
        {
            offset++;
        }

        return blob;
    }

    private static int ReadInt32(byte[] packet, ref int offset)
    {
        var value = BinaryPrimitives.ReadInt32BigEndian(packet.AsSpan(offset, 4));
        offset += 4;
        return value;
    }

    private static long ReadInt64(byte[] packet, ref int offset)
    {
        var value = BinaryPrimitives.ReadInt64BigEndian(packet.AsSpan(offset, 8));
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
