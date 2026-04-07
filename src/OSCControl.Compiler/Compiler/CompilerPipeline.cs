using OSCControl.Compiler.Diagnostics;
using OSCControl.Compiler.Execution;
using OSCControl.Compiler.Lexing;
using OSCControl.Compiler.Lowering;
using OSCControl.Compiler.Runtime;
using OSCControl.Compiler.Syntax;
using OSCControl.Compiler.Text;
using OSCControl.Compiler.Validation;

namespace OSCControl.Compiler.Compiler;

public sealed class CompilerPipeline
{
    public CompilationResult Compile(string source, LanguageVersion version = LanguageVersion.V0_1)
    {
        var sourceText = new SourceText(source);

        var tokenizer = new Tokenizer(sourceText);
        var tokens = tokenizer.Tokenize();

        var parser = new Parser(tokens);
        var syntax = parser.ParseProgram();

        var diagnostics = new DiagnosticBag();
        diagnostics.AddRange(tokenizer.Diagnostics);
        diagnostics.AddRange(parser.Diagnostics);

        var validator = new Validator();
        diagnostics.AddRange(validator.Validate(syntax, version));

        LoweredProgram? lowered = null;
        ExecutionProgram? execution = null;
        RuntimePlan? plan = null;

        if (!diagnostics.HasErrors)
        {
            lowered = new Lowerer().Lower(syntax);
            execution = new ExecutionLowerer().Lower(lowered);
            plan = new RuntimePlanner().Plan(execution);
        }

        return new CompilationResult(syntax, lowered, execution, plan, tokens, diagnostics.Items.ToArray());
    }
}
