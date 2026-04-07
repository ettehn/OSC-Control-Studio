using OSCControl.Compiler.Compiler;
using OSCControl.Compiler.Diagnostics;
using OSCControl.Compiler.Execution;
using OSCControl.Compiler.Lexing;
using OSCControl.Compiler.Lowering;
using OSCControl.Compiler.Runtime;
using OSCControl.Compiler.Syntax;
using OSCControl.Compiler.Text;
using Xunit;

namespace OSCControl.Compiler.Tests;

public sealed class CompilerPipelineTests
{
    [Fact]
    public void Compile_ValidProgram_HasNoErrors()
    {
        const string source = """
endpoint oscIn: osc.udp {
    mode: input
    host: "127.0.0.1"
    port: 9000
    codec: osc
}

state lastNote = 0

on receive oscIn when msg.body.value == 1 [
    send oscIn {
        args: [[1, 2, 3]]
    }
]
""";

        var result = new CompilerPipeline().Compile(source);

        Assert.False(result.HasErrors);

        var lowered = Assert.IsType<LoweredProgram>(result.Lowered);
        Assert.Single(lowered.Endpoints);
        Assert.Single(lowered.States);
        Assert.Single(lowered.Rules);

        var loweredRule = lowered.Rules[0];
        var loweredSend = Assert.IsType<LoweredSendStep>(Assert.Single(loweredRule.Steps));
        Assert.Equal("oscIn", loweredSend.Target);
        var loweredArgs = Assert.Single(loweredSend.Payload, p => p.Key == "args");
        var loweredList = Assert.IsType<LoweredListExpression>(loweredArgs.Value);
        Assert.Equal(3, loweredList.Items.Count);

        var execution = Assert.IsType<ExecutionProgram>(result.Execution);
        Assert.Single(execution.Endpoints);
        Assert.Single(execution.States);
        Assert.Single(execution.Rules);

        var executionRule = execution.Rules[0];
        var transportSend = Assert.IsType<ExecutionTransportSendStep>(Assert.Single(executionRule.Steps));
        Assert.Equal("oscIn", transportSend.TargetEndpoint);
        var executionArgs = Assert.IsType<ExecutionListExpression>(transportSend.Message.Args);
        Assert.Equal(3, executionArgs.Items.Count);

        var plan = Assert.IsType<RuntimePlan>(result.Plan);
        Assert.Single(plan.Endpoints);
        Assert.Single(plan.States);
        Assert.Single(plan.Rules);

        var rulePlan = plan.Rules[0];
        var receiveTrigger = Assert.IsType<RuntimeReceiveTriggerPlan>(rulePlan.Trigger);
        Assert.Equal("oscIn", receiveTrigger.EndpointName);
        var sendPlan = Assert.IsType<RuntimeTransportSendPlan>(Assert.Single(rulePlan.Steps));
        Assert.Equal("oscIn", sendPlan.TargetEndpoint);
        var runtimeArgs = Assert.IsType<RuntimeListPlan>(sendPlan.Message.Args);
        Assert.Equal(3, runtimeArgs.Items.Count);
    }

    [Fact]
    public void Tokenizer_SplitsMemberAccess_IntoSeparateTokens()
    {
        var tokenizer = new Tokenizer(new SourceText("msg.body.value"));

        var tokens = tokenizer.Tokenize().TakeWhile(t => t.Kind != TokenKind.EndOfFile).ToArray();

        Assert.Collection(
            tokens,
            token => Assert.Equal(TokenKind.Identifier, token.Kind),
            token => Assert.Equal(TokenKind.Dot, token.Kind),
            token => Assert.Equal(TokenKind.KeywordBody, token.Kind),
            token => Assert.Equal(TokenKind.Dot, token.Kind),
            token => Assert.Equal(TokenKind.Identifier, token.Kind));
    }

    [Fact]
    public void Tokenizer_Recognizes_DoubleBracket_ListTokens()
    {
        var tokenizer = new Tokenizer(new SourceText("[[1, 2]]"));

        var tokens = tokenizer.Tokenize().TakeWhile(t => t.Kind != TokenKind.EndOfFile).Select(t => t.Kind).ToArray();

        Assert.Equal(
            [TokenKind.LeftDoubleBracket, TokenKind.Number, TokenKind.Comma, TokenKind.Number, TokenKind.RightDoubleBracket],
            tokens);
    }

    [Fact]
    public void Compile_FunctionDeclaration_LowersToRuntimePlan()
    {
        const string source = """
func helper(value) [
    log info value
]

on startup [
    call helper("ok")
]
""";

        var result = new CompilerPipeline().Compile(source);

        Assert.False(result.HasErrors);
        var plan = Assert.IsType<RuntimePlan>(result.Plan);
        var function = Assert.Single(plan.Functions);
        Assert.Equal("helper", function.Name);
        Assert.Equal(["value"], function.Parameters);
        Assert.IsType<RuntimeLogPlan>(Assert.Single(function.Steps));
        var call = Assert.IsType<RuntimeInvokePlan>(Assert.Single(Assert.Single(plan.Rules).Steps));
        Assert.Equal("helper", call.Name);
    }

    [Fact]
    public void Compile_TimerTrigger_IsRejectedByV01Validator()
    {
        const string source = """
on timer 100 [
    stop
]
""";

        var result = new CompilerPipeline().Compile(source);

        var error = Assert.Single(result.Diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains("Timer triggers", error.Message);
    }

    [Fact]
    public void Compile_ForEach_LowersAcrossAllIrStages()
    {
        const string source = """
on startup [
    for item in range(0, 3) [
        log info item
    ]
]
""";

        var result = new CompilerPipeline().Compile(source);

        Assert.False(result.HasErrors);

        var lowered = Assert.IsType<LoweredProgram>(result.Lowered);
        var loweredLoop = Assert.IsType<LoweredForEachStep>(Assert.Single(lowered.Rules[0].Steps));
        Assert.Equal("item", loweredLoop.IteratorName);

        var execution = Assert.IsType<ExecutionProgram>(result.Execution);
        var executionLoop = Assert.IsType<ExecutionForEachStep>(Assert.Single(execution.Rules[0].Steps));
        Assert.Equal("item", executionLoop.IteratorName);

        var plan = Assert.IsType<RuntimePlan>(result.Plan);
        var runtimeLoop = Assert.IsType<RuntimeForEachPlan>(Assert.Single(plan.Rules[0].Steps));
        Assert.Equal("item", runtimeLoop.IteratorName);
    }

    [Fact]
    public void Compile_VrchatSugar_LowersToNormalSendSteps_AndImplicitEndpoints()
    {
        const string source = """
on startup [
    vrchat.param GestureLeft = 3
    vrchat.input Jump = 1
    vrchat.chat "Hello" send=true notify=false
    vrchat.typing true
]
""";

        var result = new CompilerPipeline().Compile(source);

        Assert.False(result.HasErrors);

        var lowered = Assert.IsType<LoweredProgram>(result.Lowered);
        Assert.Equal(2, lowered.Endpoints.Count);
        Assert.Contains(lowered.Endpoints, endpoint => endpoint.Name == "vrchat");
        Assert.Contains(lowered.Endpoints, endpoint => endpoint.Name == "vrchat_in");

        var rule = Assert.Single(lowered.Rules);
        Assert.Equal(4, rule.Steps.Count);

        var paramSend = Assert.IsType<LoweredSendStep>(rule.Steps[0]);
        Assert.Equal("vrchat", paramSend.Target);
        Assert.Equal("/avatar/parameters/GestureLeft", Assert.IsType<LoweredStringExpression>(Assert.Single(paramSend.Payload, p => p.Key == "address").Value).Value);

        var inputSend = Assert.IsType<LoweredSendStep>(rule.Steps[1]);
        Assert.Equal("/input/Jump", Assert.IsType<LoweredStringExpression>(Assert.Single(inputSend.Payload, p => p.Key == "address").Value).Value);

        var chatSend = Assert.IsType<LoweredSendStep>(rule.Steps[2]);
        Assert.Equal("/chatbox/input", Assert.IsType<LoweredStringExpression>(Assert.Single(chatSend.Payload, p => p.Key == "address").Value).Value);
        var chatArgs = Assert.IsType<LoweredListExpression>(Assert.Single(chatSend.Payload, p => p.Key == "args").Value);
        Assert.Equal(3, chatArgs.Items.Count);

        var typingSend = Assert.IsType<LoweredSendStep>(rule.Steps[3]);
        Assert.Equal("/chatbox/typing", Assert.IsType<LoweredStringExpression>(Assert.Single(typingSend.Payload, p => p.Key == "address").Value).Value);
    }

    [Fact]
    public void Compile_VrchatEndpointDeclaration_ExpandsToInputAndOutputEndpoints()
    {
        const string source = """
vrchat.endpoint {
    host: "127.0.0.1"
    inputPort: 9000
    outputPort: 9001
}

on vrchat.avatar_change [
    log info "changed"
]
""";

        var result = new CompilerPipeline().Compile(source);

        Assert.False(result.HasErrors);

        var lowered = Assert.IsType<LoweredProgram>(result.Lowered);
        var output = Assert.Single(lowered.Endpoints.Where(endpoint => endpoint.Name == "vrchat"));
        var input = Assert.Single(lowered.Endpoints.Where(endpoint => endpoint.Name == "vrchat_in"));

        Assert.Equal("output", Assert.IsType<LoweredIdentifierExpression>(Assert.Single(output.Config, p => p.Key == "mode").Value).Name);
        Assert.Equal(9001, Assert.IsType<LoweredNumberExpression>(Assert.Single(output.Config, p => p.Key == "port").Value).Value);
        Assert.Equal("input", Assert.IsType<LoweredIdentifierExpression>(Assert.Single(input.Config, p => p.Key == "mode").Value).Name);
        Assert.Equal(9000, Assert.IsType<LoweredNumberExpression>(Assert.Single(input.Config, p => p.Key == "port").Value).Value);
    }

    [Fact]
    public void Compile_VrchatAvatarChangeTrigger_LowersToAddressTrigger()
    {
        const string source = """
on vrchat.avatar_change [
    log info "changed"
]
""";

        var result = new CompilerPipeline().Compile(source);

        Assert.False(result.HasErrors);

        var lowered = Assert.IsType<LoweredProgram>(result.Lowered);
        var rule = Assert.Single(lowered.Rules);
        var trigger = Assert.IsType<LoweredAddressTrigger>(rule.Trigger);
        Assert.Equal("/avatar/change", trigger.Address);

        var plan = Assert.IsType<RuntimePlan>(result.Plan);
        var rulePlan = Assert.Single(plan.Rules);
        var runtimeTrigger = Assert.IsType<RuntimeAddressTriggerPlan>(rulePlan.Trigger);
        Assert.Equal("/avatar/change", runtimeTrigger.Address);
    }

    [Fact]
    public void Compile_VrchatParamTrigger_LowersToAvatarParameterAddress()
    {
        const string source = """
on vrchat.param GestureLeft [
    log info arg(0)
]
""";

        var result = new CompilerPipeline().Compile(source);

        Assert.False(result.HasErrors);

        var lowered = Assert.IsType<LoweredProgram>(result.Lowered);
        var rule = Assert.Single(lowered.Rules);
        var trigger = Assert.IsType<LoweredAddressTrigger>(rule.Trigger);
        Assert.Equal("/avatar/parameters/GestureLeft", trigger.Address);
    }
    [Fact]
    public void Compile_WhileBreakContinue_LowersAcrossAllIrStages()
    {
        const string source = """
on startup [
    while true [
        break
    ]

    for item in [[1]] [
        continue
    ]
]
""";

        var result = new CompilerPipeline().Compile(source);

        Assert.False(result.HasErrors);

        var lowered = Assert.IsType<LoweredProgram>(result.Lowered);
        Assert.IsType<LoweredWhileStep>(lowered.Rules[0].Steps[0]);
        var loweredFor = Assert.IsType<LoweredForEachStep>(lowered.Rules[0].Steps[1]);
        Assert.IsType<LoweredContinueStep>(Assert.Single(loweredFor.Body));

        var execution = Assert.IsType<ExecutionProgram>(result.Execution);
        Assert.IsType<ExecutionWhileStep>(execution.Rules[0].Steps[0]);
        var executionFor = Assert.IsType<ExecutionForEachStep>(execution.Rules[0].Steps[1]);
        Assert.IsType<ExecutionContinueStep>(Assert.Single(executionFor.Body));

        var plan = Assert.IsType<RuntimePlan>(result.Plan);
        Assert.IsType<RuntimeWhilePlan>(plan.Rules[0].Steps[0]);
        var runtimeFor = Assert.IsType<RuntimeForEachPlan>(plan.Rules[0].Steps[1]);
        Assert.IsType<RuntimeContinuePlan>(Assert.Single(runtimeFor.Body));
    }

    [Fact]
    public void Compile_BreakOutsideLoop_IsRejected()
    {
        const string source = """
on startup [
    break
]
""";

        var result = new CompilerPipeline().Compile(source);

        var error = Assert.Single(result.Diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains("'break' can only be used inside", error.Message);
    }
}
