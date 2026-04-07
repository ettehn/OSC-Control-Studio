using OSCControl.Compiler.Lowering;

namespace OSCControl.Compiler.Execution;

public sealed class ExecutionLowerer
{
    public ExecutionProgram Lower(LoweredProgram program)
    {
        var endpoints = program.Endpoints.Select(LowerEndpoint).ToList();
        var states = program.States.Select(LowerState).ToList();
        var rules = program.Rules.Select(LowerRule).ToList();
        var functions = program.Functions.Select(LowerFunction).ToList();
        return new ExecutionProgram(endpoints, states, rules, functions);
    }

    private static ExecutionEndpoint LowerEndpoint(LoweredEndpoint endpoint) =>
        new(endpoint.Name, endpoint.EndpointType, endpoint.Config.Select(LowerProperty).ToList());

    private static ExecutionStateSlot LowerState(LoweredState state) =>
        new(state.Name, LowerExpression(state.InitialValue));

    private static ExecutionRule LowerRule(LoweredRule rule) =>
        new(LowerTrigger(rule.Trigger), rule.Condition is null ? null : LowerExpression(rule.Condition), rule.Steps.Select(LowerStep).ToList());

    private static ExecutionFunction LowerFunction(LoweredFunction function) =>
        new(function.Name, function.Parameters, function.Steps.Select(LowerStep).ToList());

    private static ExecutionTrigger LowerTrigger(LoweredTrigger trigger) => trigger switch
    {
        LoweredReceiveTrigger receive => new ExecutionReceiveTrigger(receive.EndpointName),
        LoweredAddressTrigger address => new ExecutionAddressTrigger(address.Address),
        LoweredTimerTrigger timer => new ExecutionTimerTrigger(timer.Interval),
        LoweredStartupTrigger => new ExecutionStartupTrigger(),
        _ => throw new InvalidOperationException($"Unsupported lowered trigger: {trigger.GetType().Name}")
    };

    private static ExecutionStep LowerStep(LoweredStep step) => step switch
    {
        LoweredSendStep send => new ExecutionTransportSendStep(send.Target, LowerMessageTemplate(send.Payload)),
        LoweredStoreStep store => new ExecutionStateStoreStep(store.Name, LowerExpression(store.Value)),
        LoweredSetStep set => new ExecutionAssignStep(LowerExpression(set.Target), LowerExpression(set.Value)),
        LoweredLogStep log => new ExecutionLogStep(log.Level ?? "info", LowerExpression(log.Value)),
        LoweredIfStep branch => new ExecutionBranchStep(
            LowerExpression(branch.Condition),
            branch.ThenSteps.Select(LowerStep).ToList(),
            branch.ElseSteps?.Select(LowerStep).ToList()),
        LoweredForEachStep loop => new ExecutionForEachStep(loop.IteratorName, LowerExpression(loop.Source), loop.Body.Select(LowerStep).ToList()),
        LoweredWhileStep loop => new ExecutionWhileStep(LowerExpression(loop.Condition), loop.Body.Select(LowerStep).ToList()),
        LoweredBreakStep => new ExecutionBreakStep(),
        LoweredContinueStep => new ExecutionContinueStep(),
        LoweredCallStep call => new ExecutionInvokeStep(call.Name, call.Arguments.Select(LowerExpression).ToList()),
        LoweredStopStep => new ExecutionStopStep(),
        LoweredLetStep let => new ExecutionLetStep(let.Name, LowerExpression(let.Value)),
        _ => throw new InvalidOperationException($"Unsupported lowered step: {step.GetType().Name}")
    };

    private static ExecutionMessageTemplate LowerMessageTemplate(IReadOnlyList<LoweredProperty> properties)
    {
        ExecutionExpression? address = null;
        ExecutionExpression? args = null;
        ExecutionExpression? body = null;
        ExecutionExpression? headers = null;
        var extra = new List<ExecutionProperty>();

        foreach (var property in properties)
        {
            var lowered = LowerProperty(property);
            switch (property.Key)
            {
                case "address":
                    address = lowered.Value;
                    break;
                case "args":
                    args = lowered.Value;
                    break;
                case "body":
                    body = lowered.Value;
                    break;
                case "headers":
                    headers = lowered.Value;
                    break;
                default:
                    extra.Add(lowered);
                    break;
            }
        }

        return new ExecutionMessageTemplate(address, args, body, headers, extra);
    }

    private static ExecutionProperty LowerProperty(LoweredProperty property) =>
        new(property.Key, LowerExpression(property.Value));

    private static ExecutionExpression LowerExpression(LoweredExpression expression) => expression switch
    {
        LoweredIdentifierExpression identifier => new ExecutionIdentifierExpression(identifier.Name),
        LoweredNumberExpression number => new ExecutionNumberExpression(number.Value),
        LoweredStringExpression str => new ExecutionStringExpression(str.Value),
        LoweredBooleanExpression boolean => new ExecutionBooleanExpression(boolean.Value),
        LoweredNullExpression => new ExecutionNullExpression(),
        LoweredListExpression list => new ExecutionListExpression(list.Items.Select(LowerExpression).ToList()),
        LoweredObjectExpression obj => new ExecutionObjectExpression(obj.Properties.Select(LowerProperty).ToList()),
        LoweredCallExpression call => new ExecutionCallExpression(LowerExpression(call.Callee), call.Arguments.Select(LowerExpression).ToList()),
        LoweredMemberExpression member => new ExecutionMemberExpression(LowerExpression(member.Target), member.Member),
        LoweredIndexExpression index => new ExecutionIndexExpression(LowerExpression(index.Target), LowerExpression(index.Index)),
        LoweredUnaryExpression unary => new ExecutionUnaryExpression(unary.Operator, LowerExpression(unary.Operand)),
        LoweredBinaryExpression binary => new ExecutionBinaryExpression(LowerExpression(binary.Left), binary.Operator, LowerExpression(binary.Right)),
        LoweredParenthesizedExpression paren => new ExecutionParenthesizedExpression(LowerExpression(paren.Expression)),
        _ => throw new InvalidOperationException($"Unsupported lowered expression: {expression.GetType().Name}")
    };
}
