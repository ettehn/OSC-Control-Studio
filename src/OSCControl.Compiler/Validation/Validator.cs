using OSCControl.Compiler.Diagnostics;
using OSCControl.Compiler.Syntax;

namespace OSCControl.Compiler.Validation;

public sealed class Validator
{
    public IReadOnlyList<Diagnostic> Validate(ProgramSyntax program, LanguageVersion version)
    {
        var diagnostics = new DiagnosticBag();

        if (version == LanguageVersion.V0_1)
        {
            foreach (var declaration in program.Declarations)
            {
                switch (declaration)
                {
                    case FunctionDeclarationSyntax function:
                        diagnostics.ReportError(function.Span, "Functions are parsed but not supported in OSCControl v0.1.");
                        break;

                    case RuleDeclarationSyntax { Trigger: TimerTriggerSyntax timer }:
                        diagnostics.ReportError(timer.Span, "Timer triggers are parsed but not supported in OSCControl v0.1.");
                        break;
                }
            }
        }

        foreach (var declaration in program.Declarations)
        {
            ValidateDeclaration(declaration, diagnostics);
        }

        return diagnostics.Items;
    }

    private static void ValidateDeclaration(DeclarationSyntax declaration, DiagnosticBag diagnostics)
    {
        switch (declaration)
        {
            case FunctionDeclarationSyntax function:
                ValidateStatements(function.Body.Statements, diagnostics, 0);
                break;
            case RuleDeclarationSyntax rule:
                ValidateStatements(rule.Body.Statements, diagnostics, 0);
                break;
        }
    }

    private static void ValidateStatements(IReadOnlyList<StatementSyntax> statements, DiagnosticBag diagnostics, int loopDepth)
    {
        foreach (var statement in statements)
        {
            switch (statement)
            {
                case BreakStatementSyntax @break when loopDepth == 0:
                    diagnostics.ReportError(@break.Span, "'break' can only be used inside 'for' or 'while' loops.");
                    break;

                case ContinueStatementSyntax @continue when loopDepth == 0:
                    diagnostics.ReportError(@continue.Span, "'continue' can only be used inside 'for' or 'while' loops.");
                    break;

                case IfStatementSyntax @if:
                    ValidateStatements(@if.ThenBlock.Statements, diagnostics, loopDepth);
                    if (@if.ElseBlock is not null)
                    {
                        ValidateStatements(@if.ElseBlock.Statements, diagnostics, loopDepth);
                    }
                    break;

                case ForEachStatementSyntax loop:
                    ValidateStatements(loop.Body.Statements, diagnostics, loopDepth + 1);
                    break;

                case WhileStatementSyntax loop:
                    ValidateStatements(loop.Body.Statements, diagnostics, loopDepth + 1);
                    break;
            }
        }
    }
}
