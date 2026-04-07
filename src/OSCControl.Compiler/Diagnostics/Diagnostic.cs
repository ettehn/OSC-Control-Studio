using OSCControl.Compiler.Text;

namespace OSCControl.Compiler.Diagnostics;

public sealed record Diagnostic(
    DiagnosticSeverity Severity,
    string Message,
    SourceSpan Span,
    string? Hint = null);
