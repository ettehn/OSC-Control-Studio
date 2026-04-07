using System.Buffers.Binary;
using System.Text;

namespace OSCControl.Compiler.Runtime;

internal static class OscPacketEncoder
{
    public static byte[] Encode(string address, IReadOnlyList<object?> arguments)
    {
        using var stream = new MemoryStream();
        WritePaddedString(stream, address);

        var typeTags = new StringBuilder(",");
        foreach (var argument in arguments)
        {
            typeTags.Append(GetTypeTag(argument));
        }

        WritePaddedString(stream, typeTags.ToString());

        foreach (var argument in arguments)
        {
            WriteArgument(stream, argument);
        }

        return stream.ToArray();
    }

    private static char GetTypeTag(object? value)
    {
        return value switch
        {
            null => 'N',
            true => 'T',
            false => 'F',
            int _ => 'i',
            short _ => 'i',
            byte _ => 'i',
            sbyte _ => 'i',
            ushort _ => 'i',
            long _ => 'h',
            uint _ => 'h',
            ulong _ => 'h',
            float _ => 'f',
            double _ => 'd',
            decimal _ => 'd',
            string _ => 's',
            byte[] _ => 'b',
            IReadOnlyDictionary<string, object?> _ => 's',
            IDictionary<string, object?> _ => 's',
            _ => 's'
        };
    }

    private static void WriteArgument(Stream stream, object? value)
    {
        switch (value)
        {
            case null:
            case bool:
                return;

            case int int32:
                WriteInt32(stream, int32);
                return;

            case short int16:
                WriteInt32(stream, int16);
                return;

            case byte uint8:
                WriteInt32(stream, uint8);
                return;

            case sbyte int8:
                WriteInt32(stream, int8);
                return;

            case ushort uint16:
                WriteInt32(stream, uint16);
                return;

            case long int64:
                WriteInt64(stream, int64);
                return;

            case uint uint32:
                WriteInt64(stream, uint32);
                return;

            case ulong uint64:
                WriteInt64(stream, unchecked((long)uint64));
                return;

            case float float32:
                WriteFloat32(stream, float32);
                return;

            case double float64:
                WriteFloat64(stream, float64);
                return;

            case decimal decimalValue:
                WriteFloat64(stream, (double)decimalValue);
                return;

            case string text:
                WritePaddedString(stream, text);
                return;

            case byte[] blob:
                WriteBlob(stream, blob);
                return;

            default:
                WritePaddedString(stream, System.Text.Json.JsonSerializer.Serialize(value));
                return;
        }
    }

    private static void WritePaddedString(Stream stream, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        stream.Write(bytes, 0, bytes.Length);
        stream.WriteByte(0);
        WritePadding(stream, bytes.Length + 1);
    }

    private static void WriteBlob(Stream stream, byte[] value)
    {
        WriteInt32(stream, value.Length);
        stream.Write(value, 0, value.Length);
        WritePadding(stream, value.Length);
    }

    private static void WritePadding(Stream stream, int bytesWritten)
    {
        var padding = (4 - (bytesWritten % 4)) % 4;
        for (var i = 0; i < padding; i++)
        {
            stream.WriteByte(0);
        }
    }

    private static void WriteInt32(Stream stream, int value)
    {
        Span<byte> buffer = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(buffer, value);
        stream.Write(buffer);
    }

    private static void WriteInt64(Stream stream, long value)
    {
        Span<byte> buffer = stackalloc byte[8];
        BinaryPrimitives.WriteInt64BigEndian(buffer, value);
        stream.Write(buffer);
    }

    private static void WriteFloat32(Stream stream, float value)
    {
        WriteInt32(stream, BitConverter.SingleToInt32Bits(value));
    }

    private static void WriteFloat64(Stream stream, double value)
    {
        WriteInt64(stream, BitConverter.DoubleToInt64Bits(value));
    }
}



