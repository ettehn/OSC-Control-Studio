using OSCControl.Compiler.Syntax;

namespace OSCControl.Compiler.Lowering;

public sealed class Lowerer
{
    public LoweredProgram Lower(ProgramSyntax program)
    {
        var endpoints = new List<LoweredEndpoint>();
        foreach (var declaration in program.Declarations)
        {
            switch (declaration)
            {
                case EndpointDeclarationSyntax endpoint:
                    AddEndpointIfMissing(endpoints, LowerEndpoint(endpoint));
                    break;
                case VrchatEndpointDeclarationSyntax vrchatEndpoint:
                    AddVrchatEndpoints(endpoints, vrchatEndpoint.Config);
                    break;
            }
        }

        if (UsesVrchat(program))
        {
            AddVrchatEndpoints(endpoints, null);
        }

        var states = program.Declarations
            .OfType<StateDeclarationSyntax>()
            .Select(LowerState)
            .ToList();

        var rules = program.Declarations
            .OfType<RuleDeclarationSyntax>()
            .Select(LowerRule)
            .ToList();

        return new LoweredProgram(endpoints, states, rules);
    }

    private static void AddVrchatEndpoints(ICollection<LoweredEndpoint> endpoints, ObjectLiteralExpressionSyntax? config)
    {
        AddEndpointIfMissing(endpoints, new LoweredEndpoint("vrchat", "osc.udp", BuildVrchatConfig(config, isInput: false)));
        AddEndpointIfMissing(endpoints, new LoweredEndpoint("vrchat_in", "osc.udp", BuildVrchatConfig(config, isInput: true)));
    }

    private static IReadOnlyList<LoweredProperty> BuildVrchatConfig(ObjectLiteralExpressionSyntax? config, bool isInput)
    {
        var properties = config?.Properties ?? [];
        return
        [
            new LoweredProperty("mode", new LoweredIdentifierExpression(isInput ? "input" : "output")),
            new LoweredProperty("host", LowerPropertyOrDefault(properties, "host", new LoweredStringExpression("127.0.0.1"))),
            new LoweredProperty("port", LowerVrchatPort(properties, isInput)),
            new LoweredProperty("codec", LowerPropertyOrDefault(properties, "codec", new LoweredIdentifierExpression("osc")))
        ];
    }

    private static LoweredExpression LowerVrchatPort(IReadOnlyList<ObjectPropertySyntax> properties, bool isInput)
    {
        var keys = isInput
            ? new[] { "inputPort", "inPort" }
            : new[] { "outputPort", "outPort", "port" };

        foreach (var key in keys)
        {
            if (TryGetProperty(properties, key, out var value))
            {
                return LowerExpression(value);
            }
        }

        return new LoweredNumberExpression(isInput ? 9000 : 9001);
    }

    private static LoweredExpression LowerPropertyOrDefault(IReadOnlyList<ObjectPropertySyntax> properties, string key, LoweredExpression fallback) =>
        TryGetProperty(properties, key, out var value) ? LowerExpression(value) : fallback;

    private static bool TryGetProperty(IReadOnlyList<ObjectPropertySyntax> properties, string key, out ExpressionSyntax value)
    {
        var property = properties.FirstOrDefault(candidate => string.Equals(candidate.Key, key, StringComparison.OrdinalIgnoreCase));
        if (property is null)
        {
            value = null!;
            return false;
        }

        value = property.Value;
        return true;
    }

    private static void AddEndpointIfMissing(ICollection<LoweredEndpoint> endpoints, LoweredEndpoint endpoint)
    {
        if (endpoints.Any(existing => string.Equals(existing.Name, endpoint.Name, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        endpoints.Add(endpoint);
    }

    private static bool UsesVrchat(ProgramSyntax program)
    {
        foreach (var declaration in program.Declarations)
        {
            switch (declaration)
            {
                case VrchatEndpointDeclarationSyntax:
                    return true;
                case RuleDeclarationSyntax rule when UsesVrchat(rule):
                    return true;
            }
        }

        return false;
    }

    private static bool UsesVrchat(RuleDeclarationSyntax rule)
    {
        if (rule.Trigger is VrchatAvatarChangeTriggerSyntax or VrchatAvatarParameterTriggerSyntax)
        {
            return true;
        }

        return UsesVrchat(rule.Body.Statements);
    }

    private static bool UsesVrchat(IReadOnlyList<StatementSyntax> statements)
    {
        foreach (var statement in statements)
        {
            switch (statement)
            {
                case VrchatAvatarParameterStatementSyntax:
                case VrchatInputStatementSyntax:
                case VrchatChatStatementSyntax:
                case VrchatTypingStatementSyntax:
                    return true;
                case IfStatementSyntax @if when UsesVrchat(@if.ThenBlock.Statements) || (@if.ElseBlock is not null && UsesVrchat(@if.ElseBlock.Statements)):
                    return true;
                case ForEachStatementSyntax loop when UsesVrchat(loop.Body.Statements):
                    return true;
                case WhileStatementSyntax loop when UsesVrchat(loop.Body.Statements):
                    return true;
            }
        }

        return false;
    }

    private static LoweredEndpoint LowerEndpoint(EndpointDeclarationSyntax endpoint) =>
        new(endpoint.Name.Name, endpoint.EndpointType, LowerProperties(endpoint.Config.Properties));

    private static LoweredState LowerState(StateDeclarationSyntax state) =>
        new(state.Name.Name, LowerExpression(state.Value));

    private static LoweredRule LowerRule(RuleDeclarationSyntax rule) =>
        new(LowerTrigger(rule.Trigger), rule.Condition is null ? null : LowerExpression(rule.Condition), LowerSteps(rule.Body.Statements));

    private static LoweredTrigger LowerTrigger(TriggerSyntax trigger) => trigger switch
    {
        ReceiveTriggerSyntax receive => new LoweredReceiveTrigger(receive.EndpointName.Name),
        AddressTriggerSyntax address => new LoweredAddressTrigger(address.Value.Value),
        TimerTriggerSyntax timer => new LoweredTimerTrigger(timer.Interval.Value),
        StartupTriggerSyntax => new LoweredStartupTrigger(),
        VrchatAvatarChangeTriggerSyntax => new LoweredAddressTrigger("/avatar/change"),
        VrchatAvatarParameterTriggerSyntax parameter => new LoweredAddressTrigger($"/avatar/parameters/{parameter.ParameterName.Name}"),
        _ => throw new InvalidOperationException($"Unsupported trigger type: {trigger.GetType().Name}")
    };

    private static IReadOnlyList<LoweredStep> LowerSteps(IReadOnlyList<StatementSyntax> statements) =>
        statements.Select(LowerStep).ToList();

    private static LoweredStep LowerStep(StatementSyntax statement) => statement switch
    {
        SendStatementSyntax send => new LoweredSendStep(send.Target.Name, send.Payload is null ? [] : LowerProperties(send.Payload.Properties)),
        SetStatementSyntax set => new LoweredSetStep(LowerExpression(set.Target), LowerExpression(set.Value)),
        StoreStatementSyntax store => new LoweredStoreStep(store.Name.Name, LowerExpression(store.Value)),
        LogStatementSyntax log => new LoweredLogStep(log.Level, LowerExpression(log.Value)),
        CallStatementSyntax call => new LoweredCallStep(call.Name.Name, call.Arguments.Select(LowerExpression).ToList()),
        StopStatementSyntax => new LoweredStopStep(),
        LetStatementSyntax let => new LoweredLetStep(let.Name.Name, LowerExpression(let.Value)),
        IfStatementSyntax @if => new LoweredIfStep(
            LowerExpression(@if.Condition),
            LowerSteps(@if.ThenBlock.Statements),
            @if.ElseBlock is null ? null : LowerSteps(@if.ElseBlock.Statements)),
        ForEachStatementSyntax loop => new LoweredForEachStep(loop.Iterator.Name, LowerExpression(loop.Source), LowerSteps(loop.Body.Statements)),
        WhileStatementSyntax loop => new LoweredWhileStep(LowerExpression(loop.Condition), LowerSteps(loop.Body.Statements)),
        BreakStatementSyntax => new LoweredBreakStep(),
        ContinueStatementSyntax => new LoweredContinueStep(),
        VrchatAvatarParameterStatementSyntax vrchatParam => LowerVrchatAvatarParam(vrchatParam),
        VrchatInputStatementSyntax vrchatInput => LowerVrchatInput(vrchatInput),
        VrchatChatStatementSyntax vrchatChat => LowerVrchatChat(vrchatChat),
        VrchatTypingStatementSyntax vrchatTyping => LowerVrchatTyping(vrchatTyping),
        _ => throw new InvalidOperationException($"Unsupported statement type: {statement.GetType().Name}")
    };

    private static LoweredSendStep LowerVrchatAvatarParam(VrchatAvatarParameterStatementSyntax statement) =>
        new("vrchat", [
            new LoweredProperty("address", new LoweredStringExpression($"/avatar/parameters/{statement.ParameterName.Name}")),
            new LoweredProperty("args", new LoweredListExpression([LowerExpression(statement.Value)]))
        ]);

    private static LoweredSendStep LowerVrchatInput(VrchatInputStatementSyntax statement) =>
        new("vrchat", [
            new LoweredProperty("address", new LoweredStringExpression($"/input/{statement.InputName.Name}")),
            new LoweredProperty("args", new LoweredListExpression([LowerExpression(statement.Value)]))
        ]);

    private static LoweredSendStep LowerVrchatChat(VrchatChatStatementSyntax statement) =>
        new("vrchat", [
            new LoweredProperty("address", new LoweredStringExpression("/chatbox/input")),
            new LoweredProperty("args", new LoweredListExpression([
                LowerExpression(statement.Text),
                statement.SendValue is null ? new LoweredBooleanExpression(true) : LowerExpression(statement.SendValue),
                statement.NotifyValue is null ? new LoweredBooleanExpression(true) : LowerExpression(statement.NotifyValue)
            ]))
        ]);

    private static LoweredSendStep LowerVrchatTyping(VrchatTypingStatementSyntax statement) =>
        new("vrchat", [
            new LoweredProperty("address", new LoweredStringExpression("/chatbox/typing")),
            new LoweredProperty("args", new LoweredListExpression([LowerExpression(statement.Value)]))
        ]);

    private static IReadOnlyList<LoweredProperty> LowerProperties(IReadOnlyList<ObjectPropertySyntax> properties) =>
        properties.Select(property => new LoweredProperty(property.Key, LowerExpression(property.Value))).ToList();

    private static LoweredExpression LowerExpression(ExpressionSyntax expression) => expression switch
    {
        IdentifierSyntax identifier => new LoweredIdentifierExpression(identifier.Name),
        NumberLiteralExpressionSyntax number => new LoweredNumberExpression(number.Value),
        StringLiteralExpressionSyntax str => new LoweredStringExpression(str.Value),
        BooleanLiteralExpressionSyntax boolean => new LoweredBooleanExpression(boolean.Value),
        NullLiteralExpressionSyntax => new LoweredNullExpression(),
        ListLiteralExpressionSyntax list => new LoweredListExpression(list.Items.Select(LowerExpression).ToList()),
        ObjectLiteralExpressionSyntax obj => new LoweredObjectExpression(LowerProperties(obj.Properties)),
        CallExpressionSyntax call => new LoweredCallExpression(LowerExpression(call.Callee), call.Arguments.Select(LowerExpression).ToList()),
        MemberExpressionSyntax member => new LoweredMemberExpression(LowerExpression(member.Target), member.Member.Name),
        IndexExpressionSyntax index => new LoweredIndexExpression(LowerExpression(index.Target), LowerExpression(index.Index)),
        UnaryExpressionSyntax unary => new LoweredUnaryExpression(unary.Operator, LowerExpression(unary.Operand)),
        BinaryExpressionSyntax binary => new LoweredBinaryExpression(LowerExpression(binary.Left), binary.Operator, LowerExpression(binary.Right)),
        ParenthesizedExpressionSyntax paren => new LoweredParenthesizedExpression(LowerExpression(paren.Expression)),
        _ => throw new InvalidOperationException($"Unsupported expression type: {expression.GetType().Name}")
    };
}
