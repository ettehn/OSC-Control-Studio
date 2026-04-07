namespace OSCControl.Packaging;

public sealed class PackageBuildResult
{
    public required string AppRoot { get; init; }

    public required string AppFolder { get; init; }

    public required string ManifestPath { get; init; }

    public required string ScriptPath { get; init; }

    public required string PlanPath { get; init; }

    public required string RunCommandPath { get; init; }

    public bool HostCopied { get; init; }
}