using OSCControl.Compiler.Text;

namespace OSCControl.Compiler.Diagnostics;

public sealed class DiagnosticBag
{
    private readonly List<Diagnostic> _items = [];

    public IReadOnlyList<Diagnostic> Items => _items;

    public bool HasErrors => _items.Any(item => item.Severity == DiagnosticSeverity.Error);

    public void Add(Diagnostic diagnostic) => _items.Add(diagnostic);

    public void AddRange(IEnumerable<Diagnostic> diagnostics) => _items.AddRange(diagnostics);

    public void ReportError(SourceSpan span, string message, string? hint = null) =>
        _items.Add(new Diagnostic(DiagnosticSeverity.Error, message, span, hint));

    public void ReportWarning(SourceSpan span, string message, string? hint = null) =>
        _items.Add(new Diagnostic(DiagnosticSeverity.Warning, message, span, hint));
}
