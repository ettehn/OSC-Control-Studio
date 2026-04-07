using System.Collections.ObjectModel;

namespace OSCControl.Compiler.Runtime;

public sealed class RuntimeEventMessage
{
    public RuntimeEventMessage(
        string sourceEndpoint,
        string? address = null,
        IReadOnlyList<object?>? args = null,
        object? body = null,
        IReadOnlyDictionary<string, object?>? headers = null,
        IReadOnlyDictionary<string, object?>? extras = null)
    {
        SourceEndpoint = sourceEndpoint;
        Address = address;
        Args = RuntimeValueHelpers.CloneList(args);
        Body = RuntimeValueHelpers.CloneValue(body);
        Headers = RuntimeValueHelpers.CloneDictionary(headers);
        Extras = RuntimeValueHelpers.CloneDictionary(extras);
    }

    public string SourceEndpoint { get; set; }
    public string? Address { get; set; }
    public List<object?> Args { get; set; }
    public object? Body { get; set; }
    public Dictionary<string, object?> Headers { get; set; }
    public Dictionary<string, object?> Extras { get; set; }

    public RuntimeEventMessage Clone() =>
        new(SourceEndpoint, Address, Args, Body, Headers, Extras);
}

public sealed record RuntimeOutboundMessage(
    string TargetEndpoint,
    string? Address,
    IReadOnlyList<object?> Args,
    object? Body,
    IReadOnlyDictionary<string, object?> Headers,
    IReadOnlyDictionary<string, object?> Extras);

public sealed record RuntimeResolvedEndpoint(
    string Name,
    string TransportKind,
    IReadOnlyDictionary<string, object?> Config);

public sealed record RuntimeTransportRecord(
    DateTimeOffset Timestamp,
    string EndpointName,
    string TransportKind,
    RuntimeOutboundMessage Message);

public interface IRuntimeTransportDispatcher
{
    Task DispatchAsync(RuntimeResolvedEndpoint endpoint, RuntimeOutboundMessage message, CancellationToken cancellationToken);
}

public sealed class RecordingTransportDispatcher : IRuntimeTransportDispatcher
{
    private readonly List<RuntimeTransportRecord> _records = [];
    private readonly IRuntimeClock _clock;

    public RecordingTransportDispatcher(IRuntimeClock? clock = null)
    {
        _clock = clock ?? new SystemRuntimeClock();
    }

    public IReadOnlyList<RuntimeTransportRecord> Records => _records;

    public Task DispatchAsync(RuntimeResolvedEndpoint endpoint, RuntimeOutboundMessage message, CancellationToken cancellationToken)
    {
        _records.Add(new RuntimeTransportRecord(_clock.UtcNow, endpoint.Name, endpoint.TransportKind, message));
        return Task.CompletedTask;
    }
}

public sealed record RuntimeLogEntry(DateTimeOffset Timestamp, string Level, object? Value);

public interface IRuntimeLogSink
{
    void Write(RuntimeLogEntry entry);
}

public sealed class RecordingRuntimeLogSink : IRuntimeLogSink
{
    private readonly List<RuntimeLogEntry> _entries = [];

    public IReadOnlyList<RuntimeLogEntry> Entries => _entries;

    public void Write(RuntimeLogEntry entry)
    {
        _entries.Add(entry);
    }
}

public interface IRuntimeClock
{
    DateTimeOffset UtcNow { get; }
}

public sealed class SystemRuntimeClock : IRuntimeClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}

public sealed record RuntimeCommandContext(
    RuntimeStateStore State,
    RuntimeEventMessage? Message,
    IReadOnlyDictionary<string, object?> Locals,
    IRuntimeClock Clock);

public interface IRuntimeCommandInvoker
{
    Task InvokeAsync(string name, IReadOnlyList<object?> arguments, RuntimeCommandContext context, CancellationToken cancellationToken);
}

public sealed class DefaultRuntimeCommandInvoker : IRuntimeCommandInvoker
{
    public async Task InvokeAsync(string name, IReadOnlyList<object?> arguments, RuntimeCommandContext context, CancellationToken cancellationToken)
    {
        switch (name)
        {
            case "clear":
                if (arguments.Count != 1)
                {
                    throw new InvalidOperationException("clear expects exactly one argument.");
                }

                context.State.Clear(RuntimeValueHelpers.ToStringValue(arguments[0]));
                return;

            case "wait":
                if (arguments.Count != 1)
                {
                    throw new InvalidOperationException("wait expects exactly one argument.");
                }

                var milliseconds = Math.Max(0, Convert.ToInt32(RuntimeValueHelpers.ToNumber(arguments[0])));
                await Task.Delay(milliseconds, cancellationToken);
                return;

            default:
                throw new InvalidOperationException($"Unknown runtime command '{name}'.");
        }
    }
}

public sealed class RuntimeEngineOptions
{
    public IRuntimeTransportDispatcher? TransportDispatcher { get; init; }
    public IRuntimeLogSink? LogSink { get; init; }
    public IRuntimeCommandInvoker? CommandInvoker { get; init; }
    public IRuntimeClock? Clock { get; init; }
}

public sealed class RuntimeHostOptions
{
    public IRuntimeHostErrorSink? ErrorSink { get; init; }
}

public interface IRuntimeHostErrorSink
{
    void Report(RuntimeHostError error);
}

public sealed class RecordingRuntimeHostErrorSink : IRuntimeHostErrorSink
{
    private readonly List<RuntimeHostError> _errors = [];

    public IReadOnlyList<RuntimeHostError> Errors => _errors;

    public void Report(RuntimeHostError error)
    {
        _errors.Add(error);
    }
}

public sealed record RuntimeHostError(
    string EndpointName,
    string TransportKind,
    string Stage,
    Exception Exception,
    DateTimeOffset Timestamp);

public sealed class RuntimeStateStore
{
    private readonly Dictionary<string, object?> _values = new(StringComparer.Ordinal);

    public bool Contains(string name) => _values.ContainsKey(name);

    public object? Load(string name) =>
        _values.TryGetValue(name, out var value)
            ? RuntimeValueHelpers.CloneValue(value)
            : null;

    public void Store(string name, object? value)
    {
        _values[name] = RuntimeValueHelpers.CloneValue(value);
    }

    public void Clear(string name)
    {
        _values.Remove(name);
    }

    public IReadOnlyDictionary<string, object?> Snapshot()
    {
        var copy = _values.ToDictionary(pair => pair.Key, pair => RuntimeValueHelpers.CloneValue(pair.Value), StringComparer.Ordinal);
        return new ReadOnlyDictionary<string, object?>(copy);
    }
}

public sealed record RuntimeDispatchResult(int MatchedRules, bool StopRequested);
