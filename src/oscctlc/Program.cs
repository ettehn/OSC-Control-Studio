using OSCControl.Compiler.Compiler;
using OSCControl.Compiler.Execution;
using OSCControl.Compiler.Lexing;
using OSCControl.Compiler.Lowering;
using OSCControl.Compiler.Runtime;
using OSCControl.Compiler.Syntax;

if (args.Length != 2)
{
    PrintUsage();
    return 1;
}

var command = args[0].ToLowerInvariant();
var path = Path.GetFullPath(args[1]);

if (!File.Exists(path))
{
    Console.Error.WriteLine($"File not found: {path}");
    return 1;
}

var source = await File.ReadAllTextAsync(path);
var result = new CompilerPipeline().Compile(source);

switch (command)
{
    case "check":
        WriteDiagnostics(result);
        return result.HasErrors ? 1 : 0;

    case "parse":
        WriteSummary(result, path);
        WriteDiagnostics(result);
        return result.HasErrors ? 1 : 0;

    case "tokens":
        WriteTokens(result);
        WriteDiagnostics(result);
        return result.HasErrors ? 1 : 0;

    case "ast":
        WriteAst(result.Syntax);
        WriteDiagnostics(result);
        return result.HasErrors ? 1 : 0;

    case "lowered":
        WriteLowered(result);
        WriteDiagnostics(result);
        return result.HasErrors ? 1 : 0;

    case "execution":
        WriteExecution(result);
        WriteDiagnostics(result);
        return result.HasErrors ? 1 : 0;

    case "plan":
        oscctlc.RuntimePlanWriter.Write(result.Plan);
        WriteDiagnostics(result);
        return result.HasErrors ? 1 : 0;

    case "run":
        WriteDiagnostics(result);
        return result.HasErrors ? 1 : await RunProgramAsync(result, path);
    default:
        PrintUsage();
        return 1;
}

static void WriteSummary(CompilationResult result, string path)
{
    Console.WriteLine($"File: {path}");
    Console.WriteLine($"Tokens: {result.Tokens.Count}");
    Console.WriteLine($"Declarations: {result.Syntax.Declarations.Count}");
    Console.WriteLine($"Endpoints: {result.Syntax.Declarations.OfType<EndpointDeclarationSyntax>().Count()}");
    Console.WriteLine($"States: {result.Syntax.Declarations.OfType<StateDeclarationSyntax>().Count()}");
    Console.WriteLine($"Rules: {result.Syntax.Declarations.OfType<RuleDeclarationSyntax>().Count()}");
    Console.WriteLine($"Functions: {result.Syntax.Declarations.OfType<FunctionDeclarationSyntax>().Count()}");
}

static void WriteDiagnostics(CompilationResult result)
{
    if (result.Diagnostics.Count == 0)
    {
        Console.WriteLine("No diagnostics.");
        return;
    }

    foreach (var diagnostic in result.Diagnostics)
    {
        var span = diagnostic.Span.Start;
        Console.WriteLine($"{diagnostic.Severity}: line {span.Line}, col {span.Column}: {diagnostic.Message}");
        if (!string.IsNullOrWhiteSpace(diagnostic.Hint))
        {
            Console.WriteLine($"  hint: {diagnostic.Hint}");
        }
    }
}

static void WriteTokens(CompilationResult result)
{
    foreach (var token in result.Tokens)
    {
        var span = token.Span.Start;
        var valueSuffix = token.Value is null ? string.Empty : $" = {FormatScalar(token.Value)}";
        Console.WriteLine($"{span.Line}:{span.Column}  {token.Kind}  '{token.Text}'{valueSuffix}");
    }
}

static void WriteAst(ProgramSyntax program)
{
    WriteProgram(program, 0);
}

static void WriteLowered(CompilationResult result)
{
    if (result.Lowered is null)
    {
        Console.WriteLine("No lowered program available.");
        return;
    }

    WriteLoweredProgram(result.Lowered, 0);
}

static void WriteExecution(CompilationResult result)
{
    if (result.Execution is null)
    {
        Console.WriteLine("No execution program available.");
        return;
    }

    WriteExecutionProgram(result.Execution, 0);
}

static void WriteExecutionProgram(ExecutionProgram program, int indent)
{
    WriteNode(indent, "ExecutionProgram");

    WriteNode(indent + 1, $"Endpoints ({program.Endpoints.Count})");
    foreach (var endpoint in program.Endpoints)
    {
        WriteNode(indent + 2, $"ExecutionEndpoint {endpoint.Name} : {endpoint.TransportKind}");
        foreach (var property in endpoint.Config)
        {
            WriteExecutionExpression(property.Key, property.Value, indent + 3);
        }
    }

    WriteNode(indent + 1, $"States ({program.States.Count})");
    foreach (var state in program.States)
    {
        WriteNode(indent + 2, $"ExecutionStateSlot {state.Name}");
        WriteExecutionExpression("InitialValue", state.InitialValue, indent + 3);
    }

    WriteNode(indent + 1, $"Rules ({program.Rules.Count})");
    foreach (var rule in program.Rules)
    {
        WriteNode(indent + 2, "ExecutionRule");
        WriteExecutionTrigger(rule.Trigger, indent + 3);
        if (rule.Condition is not null)
        {
            WriteExecutionExpression("Condition", rule.Condition, indent + 3);
        }
        WriteNode(indent + 3, $"Steps ({rule.Steps.Count})");
        foreach (var step in rule.Steps)
        {
            WriteExecutionStep(step, indent + 4);
        }
    }
}

static void WriteExecutionTrigger(ExecutionTrigger trigger, int indent)
{
    switch (trigger)
    {
        case ExecutionReceiveTrigger receive:
            WriteNode(indent, $"ExecutionReceiveTrigger {receive.EndpointName}");
            break;
        case ExecutionAddressTrigger address:
            WriteNode(indent, $"ExecutionAddressTrigger \"{address.Address}\"");
            break;
        case ExecutionTimerTrigger timer:
            WriteNode(indent, $"ExecutionTimerTrigger {timer.Interval}");
            break;
        case ExecutionStartupTrigger:
            WriteNode(indent, "ExecutionStartupTrigger");
            break;
    }
}

static void WriteExecutionStep(ExecutionStep step, int indent)
{
    switch (step)
    {
        case ExecutionTransportSendStep send:
            WriteNode(indent, $"ExecutionTransportSendStep {send.TargetEndpoint}");
            WriteExecutionMessageTemplate(send.Message, indent + 1);
            break;
        case ExecutionStateStoreStep store:
            WriteNode(indent, $"ExecutionStateStoreStep {store.StateName}");
            WriteExecutionExpression("Value", store.Value, indent + 1);
            break;
        case ExecutionAssignStep assign:
            WriteNode(indent, "ExecutionAssignStep");
            WriteExecutionExpression("Target", assign.Target, indent + 1);
            WriteExecutionExpression("Value", assign.Value, indent + 1);
            break;
        case ExecutionLogStep log:
            WriteNode(indent, $"ExecutionLogStep {log.Level}");
            WriteExecutionExpression("Value", log.Value, indent + 1);
            break;
        case ExecutionBranchStep branch:
            WriteNode(indent, "ExecutionBranchStep");
            WriteExecutionExpression("Condition", branch.Condition, indent + 1);
            WriteNode(indent + 1, $"ThenSteps ({branch.ThenSteps.Count})");
            foreach (var nested in branch.ThenSteps)
            {
                WriteExecutionStep(nested, indent + 2);
            }
            if (branch.ElseSteps is not null)
            {
                WriteNode(indent + 1, $"ElseSteps ({branch.ElseSteps.Count})");
                foreach (var nested in branch.ElseSteps)
                {
                    WriteExecutionStep(nested, indent + 2);
                }
            }
            break;
        case ExecutionInvokeStep invoke:
            WriteNode(indent, $"ExecutionInvokeStep {invoke.Name}");
            for (var i = 0; i < invoke.Arguments.Count; i++)
            {
                WriteExecutionExpression($"Arg[{i}]", invoke.Arguments[i], indent + 1);
            }
            break;
        case ExecutionStopStep:
            WriteNode(indent, "ExecutionStopStep");
            break;
        case ExecutionLetStep let:
            WriteNode(indent, $"ExecutionLetStep {let.Name}");
            WriteExecutionExpression("Value", let.Value, indent + 1);
            break;
    }
}

static void WriteExecutionMessageTemplate(ExecutionMessageTemplate message, int indent)
{
    WriteNode(indent, "Message");
    if (message.Address is not null)
    {
        WriteExecutionExpression("Address", message.Address, indent + 1);
    }
    if (message.Args is not null)
    {
        WriteExecutionExpression("Args", message.Args, indent + 1);
    }
    if (message.Body is not null)
    {
        WriteExecutionExpression("Body", message.Body, indent + 1);
    }
    if (message.Headers is not null)
    {
        WriteExecutionExpression("Headers", message.Headers, indent + 1);
    }
    foreach (var property in message.ExtraProperties)
    {
        WriteExecutionExpression(property.Key, property.Value, indent + 1);
    }
}

static void WriteExecutionExpression(string label, ExecutionExpression expression, int indent)
{
    switch (expression)
    {
        case ExecutionIdentifierExpression identifier:
            WriteNode(indent, $"{label}: ExecutionIdentifier({identifier.Name})");
            break;
        case ExecutionNumberExpression number:
            WriteNode(indent, $"{label}: ExecutionNumber({number.Value})");
            break;
        case ExecutionStringExpression str:
            WriteNode(indent, $"{label}: ExecutionString(\"{str.Value}\")");
            break;
        case ExecutionBooleanExpression boolean:
            WriteNode(indent, $"{label}: ExecutionBoolean({boolean.Value})");
            break;
        case ExecutionNullExpression:
            WriteNode(indent, $"{label}: ExecutionNull");
            break;
        case ExecutionListExpression list:
            WriteNode(indent, $"{label}: ExecutionList");
            for (var i = 0; i < list.Items.Count; i++)
            {
                WriteExecutionExpression($"Item[{i}]", list.Items[i], indent + 1);
            }
            break;
        case ExecutionObjectExpression obj:
            WriteNode(indent, $"{label}: ExecutionObject");
            foreach (var property in obj.Properties)
            {
                WriteExecutionExpression(property.Key, property.Value, indent + 1);
            }
            break;
        case ExecutionCallExpression call:
            WriteNode(indent, $"{label}: ExecutionCall");
            WriteExecutionExpression("Callee", call.Callee, indent + 1);
            for (var i = 0; i < call.Arguments.Count; i++)
            {
                WriteExecutionExpression($"Arg[{i}]", call.Arguments[i], indent + 1);
            }
            break;
        case ExecutionMemberExpression member:
            WriteNode(indent, $"{label}: ExecutionMember(.{member.Member})");
            WriteExecutionExpression("Target", member.Target, indent + 1);
            break;
        case ExecutionIndexExpression index:
            WriteNode(indent, $"{label}: ExecutionIndex");
            WriteExecutionExpression("Target", index.Target, indent + 1);
            WriteExecutionExpression("Index", index.Index, indent + 1);
            break;
        case ExecutionUnaryExpression unary:
            WriteNode(indent, $"{label}: ExecutionUnary({unary.Operator})");
            WriteExecutionExpression("Operand", unary.Operand, indent + 1);
            break;
        case ExecutionBinaryExpression binary:
            WriteNode(indent, $"{label}: ExecutionBinary({binary.Operator})");
            WriteExecutionExpression("Left", binary.Left, indent + 1);
            WriteExecutionExpression("Right", binary.Right, indent + 1);
            break;
        case ExecutionParenthesizedExpression paren:
            WriteNode(indent, $"{label}: ExecutionParenthesized");
            WriteExecutionExpression("Expression", paren.Expression, indent + 1);
            break;
    }
}

static void WriteLoweredProgram(LoweredProgram program, int indent)
{
    WriteNode(indent, "LoweredProgram");

    WriteNode(indent + 1, $"Endpoints ({program.Endpoints.Count})");
    foreach (var endpoint in program.Endpoints)
    {
        WriteNode(indent + 2, $"LoweredEndpoint {endpoint.Name} : {endpoint.EndpointType}");
        foreach (var property in endpoint.Config)
        {
            WriteLoweredExpression(property.Key, property.Value, indent + 3);
        }
    }

    WriteNode(indent + 1, $"States ({program.States.Count})");
    foreach (var state in program.States)
    {
        WriteNode(indent + 2, $"LoweredState {state.Name}");
        WriteLoweredExpression("InitialValue", state.InitialValue, indent + 3);
    }

    WriteNode(indent + 1, $"Rules ({program.Rules.Count})");
    foreach (var rule in program.Rules)
    {
        WriteNode(indent + 2, "LoweredRule");
        WriteLoweredTrigger(rule.Trigger, indent + 3);
        if (rule.Condition is not null)
        {
            WriteLoweredExpression("Condition", rule.Condition, indent + 3);
        }

        WriteNode(indent + 3, $"Steps ({rule.Steps.Count})");
        foreach (var step in rule.Steps)
        {
            WriteLoweredStep(step, indent + 4);
        }
    }
}

static void WriteLoweredTrigger(LoweredTrigger trigger, int indent)
{
    switch (trigger)
    {
        case LoweredReceiveTrigger receive:
            WriteNode(indent, $"LoweredReceiveTrigger {receive.EndpointName}");
            break;
        case LoweredAddressTrigger address:
            WriteNode(indent, $"LoweredAddressTrigger \"{address.Address}\"");
            break;
        case LoweredTimerTrigger timer:
            WriteNode(indent, $"LoweredTimerTrigger {timer.Interval}");
            break;
        case LoweredStartupTrigger:
            WriteNode(indent, "LoweredStartupTrigger");
            break;
    }
}

static void WriteLoweredStep(LoweredStep step, int indent)
{
    switch (step)
    {
        case LoweredSendStep send:
            WriteNode(indent, $"LoweredSendStep {send.Target}");
            foreach (var property in send.Payload)
            {
                WriteLoweredExpression(property.Key, property.Value, indent + 1);
            }
            break;
        case LoweredSetStep set:
            WriteNode(indent, "LoweredSetStep");
            WriteLoweredExpression("Target", set.Target, indent + 1);
            WriteLoweredExpression("Value", set.Value, indent + 1);
            break;
        case LoweredStoreStep store:
            WriteNode(indent, $"LoweredStoreStep {store.Name}");
            WriteLoweredExpression("Value", store.Value, indent + 1);
            break;
        case LoweredLogStep log:
            WriteNode(indent, $"LoweredLogStep {(log.Level ?? "default")}");
            WriteLoweredExpression("Value", log.Value, indent + 1);
            break;
        case LoweredCallStep call:
            WriteNode(indent, $"LoweredCallStep {call.Name}");
            for (var i = 0; i < call.Arguments.Count; i++)
            {
                WriteLoweredExpression($"Arg[{i}]", call.Arguments[i], indent + 1);
            }
            break;
        case LoweredStopStep:
            WriteNode(indent, "LoweredStopStep");
            break;
        case LoweredLetStep let:
            WriteNode(indent, $"LoweredLetStep {let.Name}");
            WriteLoweredExpression("Value", let.Value, indent + 1);
            break;
        case LoweredIfStep branch:
            WriteNode(indent, "LoweredIfStep");
            WriteLoweredExpression("Condition", branch.Condition, indent + 1);
            WriteNode(indent + 1, $"ThenSteps ({branch.ThenSteps.Count})");
            foreach (var nested in branch.ThenSteps)
            {
                WriteLoweredStep(nested, indent + 2);
            }
            if (branch.ElseSteps is not null)
            {
                WriteNode(indent + 1, $"ElseSteps ({branch.ElseSteps.Count})");
                foreach (var nested in branch.ElseSteps)
                {
                    WriteLoweredStep(nested, indent + 2);
                }
            }
            break;
    }
}

static void WriteLoweredExpression(string label, LoweredExpression expression, int indent)
{
    switch (expression)
    {
        case LoweredIdentifierExpression identifier:
            WriteNode(indent, $"{label}: LoweredIdentifier({identifier.Name})");
            break;
        case LoweredNumberExpression number:
            WriteNode(indent, $"{label}: LoweredNumber({number.Value})");
            break;
        case LoweredStringExpression str:
            WriteNode(indent, $"{label}: LoweredString(\"{str.Value}\")");
            break;
        case LoweredBooleanExpression boolean:
            WriteNode(indent, $"{label}: LoweredBoolean({boolean.Value})");
            break;
        case LoweredNullExpression:
            WriteNode(indent, $"{label}: LoweredNull");
            break;
        case LoweredListExpression list:
            WriteNode(indent, $"{label}: LoweredList");
            for (var i = 0; i < list.Items.Count; i++)
            {
                WriteLoweredExpression($"Item[{i}]", list.Items[i], indent + 1);
            }
            break;
        case LoweredObjectExpression obj:
            WriteNode(indent, $"{label}: LoweredObject");
            foreach (var property in obj.Properties)
            {
                WriteLoweredExpression(property.Key, property.Value, indent + 1);
            }
            break;
        case LoweredCallExpression call:
            WriteNode(indent, $"{label}: LoweredCall");
            WriteLoweredExpression("Callee", call.Callee, indent + 1);
            for (var i = 0; i < call.Arguments.Count; i++)
            {
                WriteLoweredExpression($"Arg[{i}]", call.Arguments[i], indent + 1);
            }
            break;
        case LoweredMemberExpression member:
            WriteNode(indent, $"{label}: LoweredMember(.{member.Member})");
            WriteLoweredExpression("Target", member.Target, indent + 1);
            break;
        case LoweredIndexExpression index:
            WriteNode(indent, $"{label}: LoweredIndex");
            WriteLoweredExpression("Target", index.Target, indent + 1);
            WriteLoweredExpression("Index", index.Index, indent + 1);
            break;
        case LoweredUnaryExpression unary:
            WriteNode(indent, $"{label}: LoweredUnary({unary.Operator})");
            WriteLoweredExpression("Operand", unary.Operand, indent + 1);
            break;
        case LoweredBinaryExpression binary:
            WriteNode(indent, $"{label}: LoweredBinary({binary.Operator})");
            WriteLoweredExpression("Left", binary.Left, indent + 1);
            WriteLoweredExpression("Right", binary.Right, indent + 1);
            break;
        case LoweredParenthesizedExpression paren:
            WriteNode(indent, $"{label}: LoweredParenthesized");
            WriteLoweredExpression("Expression", paren.Expression, indent + 1);
            break;
    }
}

static void WriteProgram(ProgramSyntax program, int indent)
{
    WriteNode(indent, "Program");
    foreach (var declaration in program.Declarations)
    {
        WriteDeclaration(declaration, indent + 1);
    }
}

static void WriteDeclaration(DeclarationSyntax declaration, int indent)
{
    switch (declaration)
    {
        case EndpointDeclarationSyntax endpoint:
            WriteNode(indent, $"Endpoint {endpoint.Name.Name} : {endpoint.EndpointType}");
            WriteObjectLiteral("Config", endpoint.Config, indent + 1);
            break;
        case StateDeclarationSyntax state:
            WriteNode(indent, $"State {state.Name.Name}");
            WriteExpression("Value", state.Value, indent + 1);
            break;
        case FunctionDeclarationSyntax function:
            WriteNode(indent, $"Function {function.Name.Name}");
            if (function.Parameters.Count > 0)
            {
                WriteNode(indent + 1, $"Parameters: {string.Join(", ", function.Parameters.Select(p => p.Name))}");
            }
            WriteExecBlock("Body", function.Body, indent + 1);
            break;
        case RuleDeclarationSyntax rule:
            WriteNode(indent, "Rule");
            WriteTrigger(rule.Trigger, indent + 1);
            if (rule.Condition is not null)
            {
                WriteExpression("When", rule.Condition, indent + 1);
            }
            WriteExecBlock("Body", rule.Body, indent + 1);
            break;
    }
}

static void WriteTrigger(TriggerSyntax trigger, int indent)
{
    switch (trigger)
    {
        case ReceiveTriggerSyntax receive:
            WriteNode(indent, $"Trigger Receive {receive.EndpointName.Name}");
            break;
        case AddressTriggerSyntax address:
            WriteNode(indent, $"Trigger Address \"{address.Value.Value}\"");
            break;
        case TimerTriggerSyntax timer:
            WriteNode(indent, $"Trigger Timer {timer.Interval.Value}");
            break;
        case StartupTriggerSyntax:
            WriteNode(indent, "Trigger Startup");
            break;
    }
}

static void WriteExecBlock(string label, ExecBlockSyntax block, int indent)
{
    WriteNode(indent, label);
    foreach (var statement in block.Statements)
    {
        WriteStatement(statement, indent + 1);
    }
}

static void WriteStatement(StatementSyntax statement, int indent)
{
    switch (statement)
    {
        case SendStatementSyntax send:
            WriteNode(indent, $"Send {send.Target.Name}");
            if (send.Payload is not null)
            {
                WriteObjectLiteral("Payload", send.Payload, indent + 1);
            }
            break;
        case SetStatementSyntax set:
            WriteNode(indent, "Set");
            WriteExpression("Target", set.Target, indent + 1);
            WriteExpression("Value", set.Value, indent + 1);
            break;
        case StoreStatementSyntax store:
            WriteNode(indent, $"Store {store.Name.Name}");
            WriteExpression("Value", store.Value, indent + 1);
            break;
        case LogStatementSyntax log:
            WriteNode(indent, $"Log {(log.Level ?? "default")}");
            WriteExpression("Value", log.Value, indent + 1);
            break;
        case CallStatementSyntax call:
            WriteNode(indent, $"Call {call.Name.Name}");
            for (var i = 0; i < call.Arguments.Count; i++)
            {
                WriteExpression($"Arg[{i}]", call.Arguments[i], indent + 1);
            }
            break;
        case StopStatementSyntax:
            WriteNode(indent, "Stop");
            break;
        case LetStatementSyntax let:
            WriteNode(indent, $"Let {let.Name.Name}");
            WriteExpression("Value", let.Value, indent + 1);
            break;
        case IfStatementSyntax @if:
            WriteNode(indent, "If");
            WriteExpression("Condition", @if.Condition, indent + 1);
            WriteExecBlock("Then", @if.ThenBlock, indent + 1);
            if (@if.ElseBlock is not null)
            {
                WriteExecBlock("Else", @if.ElseBlock, indent + 1);
            }
            break;
    }
}

static void WriteExpression(string label, ExpressionSyntax expression, int indent)
{
    switch (expression)
    {
        case IdentifierSyntax identifier:
            WriteNode(indent, $"{label}: Identifier({identifier.Name})");
            break;
        case NumberLiteralExpressionSyntax number:
            WriteNode(indent, $"{label}: Number({number.Value})");
            break;
        case StringLiteralExpressionSyntax str:
            WriteNode(indent, $"{label}: String(\"{str.Value}\")");
            break;
        case BooleanLiteralExpressionSyntax boolean:
            WriteNode(indent, $"{label}: Boolean({boolean.Value})");
            break;
        case NullLiteralExpressionSyntax:
            WriteNode(indent, $"{label}: Null");
            break;
        case ListLiteralExpressionSyntax list:
            WriteNode(indent, $"{label}: List");
            for (var i = 0; i < list.Items.Count; i++)
            {
                WriteExpression($"Item[{i}]", list.Items[i], indent + 1);
            }
            break;
        case ObjectLiteralExpressionSyntax obj:
            WriteNode(indent, $"{label}: Object");
            foreach (var property in obj.Properties)
            {
                WriteExpression(property.Key, property.Value, indent + 1);
            }
            break;
        case CallExpressionSyntax call:
            WriteNode(indent, $"{label}: Call");
            WriteExpression("Callee", call.Callee, indent + 1);
            for (var i = 0; i < call.Arguments.Count; i++)
            {
                WriteExpression($"Arg[{i}]", call.Arguments[i], indent + 1);
            }
            break;
        case MemberExpressionSyntax member:
            WriteNode(indent, $"{label}: Member(.{member.Member.Name})");
            WriteExpression("Target", member.Target, indent + 1);
            break;
        case IndexExpressionSyntax index:
            WriteNode(indent, $"{label}: Index");
            WriteExpression("Target", index.Target, indent + 1);
            WriteExpression("Index", index.Index, indent + 1);
            break;
        case UnaryExpressionSyntax unary:
            WriteNode(indent, $"{label}: Unary({unary.Operator})");
            WriteExpression("Operand", unary.Operand, indent + 1);
            break;
        case BinaryExpressionSyntax binary:
            WriteNode(indent, $"{label}: Binary({binary.Operator})");
            WriteExpression("Left", binary.Left, indent + 1);
            WriteExpression("Right", binary.Right, indent + 1);
            break;
        case ParenthesizedExpressionSyntax paren:
            WriteNode(indent, $"{label}: Parenthesized");
            WriteExpression("Expression", paren.Expression, indent + 1);
            break;
    }
}

static void WriteObjectLiteral(string label, ObjectLiteralExpressionSyntax obj, int indent)
{
    WriteNode(indent, label);
    foreach (var property in obj.Properties)
    {
        WriteExpression(property.Key, property.Value, indent + 1);
    }
}

static void WriteNode(int indent, string text)
{
    Console.WriteLine($"{new string(' ', indent * 2)}- {text}");
}

static string FormatScalar(object value) => value switch
{
    string text => $"\"{text}\"",
    _ => value.ToString() ?? string.Empty
};

static void PrintUsage()
{
    Console.WriteLine("Usage:");
    Console.WriteLine("  oscctlc check <file>");
    Console.WriteLine("  oscctlc parse <file>");
    Console.WriteLine("  oscctlc tokens <file>");
    Console.WriteLine("  oscctlc ast <file>");
    Console.WriteLine("  oscctlc lowered <file>");
    Console.WriteLine("  oscctlc execution <file>");
    Console.WriteLine("  oscctlc plan <file>");
    Console.WriteLine("  oscctlc run <file>");
}

static async Task<int> RunProgramAsync(CompilationResult result, string path)
{
    if (result.Plan is null)
    {
        Console.Error.WriteLine("No runtime plan available.");
        return 1;
    }

    using var cancellationSource = new CancellationTokenSource();
    var consoleLogSink = new oscctlc.ConsoleRuntimeLogSink();
    var consoleErrorSink = new oscctlc.ConsoleRuntimeHostErrorSink();

    Console.CancelKeyPress += OnCancelKeyPress;

    try
    {
        await using var engine = new RuntimeEngine(result.Plan, new RuntimeEngineOptions
        {
            LogSink = consoleLogSink
        });

        await using var host = new RuntimeHost(engine, new RuntimeHostOptions
        {
            ErrorSink = consoleErrorSink
        });

        Console.WriteLine($"Running: {path}");
        Console.WriteLine($"Endpoints: {engine.Endpoints.Count}  States: {result.Plan.States.Count}  Rules: {result.Plan.Rules.Count}");
        Console.WriteLine("Press Ctrl+C to stop.");

        await host.StartAsync(cancellationSource.Token);

        try
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationSource.Token);
        }
        catch (OperationCanceledException)
        {
        }

        await host.StopAsync();
        return 0;
    }
    catch (OperationCanceledException)
    {
        return 0;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Runtime error: {ex.Message}");
        return 1;
    }
    finally
    {
        Console.CancelKeyPress -= OnCancelKeyPress;
    }

    void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs eventArgs)
    {
        eventArgs.Cancel = true;
        cancellationSource.Cancel();
    }
}

