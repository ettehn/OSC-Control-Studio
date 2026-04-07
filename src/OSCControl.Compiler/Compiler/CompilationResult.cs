using OSCControl.Compiler.Diagnostics;
using OSCControl.Compiler.Execution;
using OSCControl.Compiler.Lexing;
using OSCControl.Compiler.Lowering;
using OSCControl.Compiler.Runtime;
using OSCControl.Compiler.Syntax;

namespace OSCControl.Compiler.Compiler;

public sealed record CompilationResult(
    ProgramSyntax Syntax,
    LoweredProgram? Lowered,
    ExecutionProgram? Execution,
    RuntimePlan? Plan,
    IReadOnlyList<Token> Tokens,
    IReadOnlyList<Diagnostic> Diagnostics)
{
    public bool HasErrors => Diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error);
}
