using OSCControl.Compiler.Diagnostics;

namespace OSCControl.Packaging;

public sealed class PackageBuildException : Exception
{
    public PackageBuildException(string message, IReadOnlyList<Diagnostic> diagnostics)
        : base(message)
    {
        Diagnostics = diagnostics;
    }

    public IReadOnlyList<Diagnostic> Diagnostics { get; }
}