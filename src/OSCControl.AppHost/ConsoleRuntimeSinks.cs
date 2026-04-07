using System.Text.Json;
using OSCControl.Compiler.Runtime;

namespace OSCControl.AppHost;

internal sealed class ConsoleRuntimeLogSink : IRuntimeLogSink
{
    private static readonly object Gate = new();

    public void Write(RuntimeLogEntry entry)
    {
        lock (Gate)
        {
            Console.WriteLine($"[{entry.Timestamp:O}] log/{entry.Level}: {FormatValue(entry.Value)}");
        }
    }

    private static string FormatValue(object? value) => value switch
    {
        null => "null",
        string text => text,
        _ => JsonSerializer.Serialize(value)
    };
}

internal sealed class ConsoleRuntimeHostErrorSink : IRuntimeHostErrorSink
{
    private static readonly object Gate = new();

    public void Report(RuntimeHostError error)
    {
        lock (Gate)
        {
            Console.Error.WriteLine($"[{error.Timestamp:O}] host/{error.Stage} {error.EndpointName} ({error.TransportKind}): {error.Exception.Message}");
        }
    }
}