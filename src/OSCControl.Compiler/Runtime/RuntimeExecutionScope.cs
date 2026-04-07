namespace OSCControl.Compiler.Runtime;

internal sealed class RuntimeExecutionScope
{
    public RuntimeExecutionScope(RuntimeStateStore state, RuntimeEventMessage? message, IRuntimeClock clock)
    {
        State = state;
        Message = message;
        Clock = clock;
        Locals = new Dictionary<string, object?>(StringComparer.Ordinal);
    }

    public RuntimeStateStore State { get; }
    public RuntimeEventMessage? Message { get; }
    public IRuntimeClock Clock { get; }
    public Dictionary<string, object?> Locals { get; }
    public bool StopRequested { get; set; }
    public bool BreakRequested { get; set; }
    public bool ContinueRequested { get; set; }
    public string? CurrentTargetEndpoint { get; set; }
}
