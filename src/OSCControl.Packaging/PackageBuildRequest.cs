namespace OSCControl.Packaging;

public sealed class PackageBuildRequest
{
    public required string Source { get; init; }

    public string? ScriptPath { get; init; }

    public required string OutputRoot { get; init; }

    public required string AppName { get; init; }

    public string? HostSource { get; init; }
}