using OSCControl.Compiler.Execution;

namespace OSCControl.Compiler.Runtime;

public sealed class RuntimePlanner
{
    public RuntimePlan Plan(ExecutionProgram program)
    {
        var endpoints = program.Endpoints.Select(PlanEndpoint).ToList();
        var states = program.States.Select(PlanState).ToList();
        var rules = program.Rules.Select((rule, index) => PlanRule(rule, index)).ToList();
        var functions = program.Functions.Select(PlanFunction).ToList();
        return new RuntimePlan(endpoints, states, rules, functions);
    }

    private static RuntimeEndpointPlan PlanEndpoint(ExecutionEndpoint endpoint) =>
        new(endpoint.Name, endpoint.TransportKind, endpoint.Config.Select(PlanProperty).ToList());

    private static RuntimeStatePlan PlanState(ExecutionStateSlot state) =>
        new(state.Name, PlanExpression(state.InitialValue));

    private static RuntimeRulePlan PlanRule(ExecutionRule rule, int index) =>
        new(index, PlanTrigger(rule.Trigger), rule.Condition is null ? null : PlanExpression(rule.Condition), rule.Steps.Select(PlanStep).ToList());

    private static RuntimeFunctionPlan PlanFunction(ExecutionFunction function) =>
        new(function.Name, function.Parameters, function.Steps.Select(PlanStep).ToList());

    private static RuntimeTriggerPlan PlanTrigger(ExecutionTrigger trigger) => trigger switch
    {
        ExecutionReceiveTrigger receive => new RuntimeReceiveTriggerPlan(receive.EndpointName),
        ExecutionAddressTrigger address => new RuntimeAddressTriggerPlan(address.Address),
        ExecutionTimerTrigger timer => new RuntimeTimerTriggerPlan(timer.Interval),
        ExecutionStartupTrigger => new RuntimeStartupTriggerPlan(),
        _ => throw new InvalidOperationException($"Unsupported execution trigger: {trigger.GetType().Name}")
    };

    private static RuntimeStepPlan PlanStep(ExecutionStep step) => step switch
    {
        ExecutionTransportSendStep send => new RuntimeTransportSendPlan(send.TargetEndpoint, PlanMessage(send.Message)),
        ExecutionStateStoreStep store => new RuntimeStateStorePlan(store.StateName, PlanExpression(store.Value)),
        ExecutionAssignStep assign => new RuntimeAssignPlan(PlanExpression(assign.Target), PlanExpression(assign.Value)),
        ExecutionLogStep log => new RuntimeLogPlan(log.Level, PlanExpression(log.Value)),
        ExecutionBranchStep branch => new RuntimeBranchPlan(
            PlanExpression(branch.Condition),
            branch.ThenSteps.Select(PlanStep).ToList(),
            branch.ElseSteps?.Select(PlanStep).ToList()),
        ExecutionForEachStep loop => new RuntimeForEachPlan(loop.IteratorName, PlanExpression(loop.Source), loop.Body.Select(PlanStep).ToList()),
        ExecutionWhileStep loop => new RuntimeWhilePlan(PlanExpression(loop.Condition), loop.Body.Select(PlanStep).ToList()),
        ExecutionBreakStep => new RuntimeBreakPlan(),
        ExecutionContinueStep => new RuntimeContinuePlan(),
        ExecutionInvokeStep invoke => new RuntimeInvokePlan(invoke.Name, invoke.Arguments.Select(PlanExpression).ToList()),
        ExecutionStopStep => new RuntimeStopPlan(),
        ExecutionLetStep let => new RuntimeLetPlan(let.Name, PlanExpression(let.Value)),
        _ => throw new InvalidOperationException($"Unsupported execution step: {step.GetType().Name}")
    };

    private static RuntimeMessagePlan PlanMessage(ExecutionMessageTemplate message) =>
        new(
            message.Address is null ? null : PlanExpression(message.Address),
            message.Args is null ? null : PlanExpression(message.Args),
            message.Body is null ? null : PlanExpression(message.Body),
            message.Headers is null ? null : PlanExpression(message.Headers),
            message.ExtraProperties.Select(PlanProperty).ToList());

    private static RuntimePropertyPlan PlanProperty(ExecutionProperty property) =>
        new(property.Key, PlanExpression(property.Value));

    private static RuntimeExpressionPlan PlanExpression(ExecutionExpression expression) => expression switch
    {
        ExecutionIdentifierExpression identifier => new RuntimeIdentifierPlan(identifier.Name),
        ExecutionNumberExpression number => new RuntimeNumberPlan(number.Value),
        ExecutionStringExpression str => new RuntimeStringPlan(str.Value),
        ExecutionBooleanExpression boolean => new RuntimeBooleanPlan(boolean.Value),
        ExecutionNullExpression => new RuntimeNullPlan(),
        ExecutionListExpression list => new RuntimeListPlan(list.Items.Select(PlanExpression).ToList()),
        ExecutionObjectExpression obj => new RuntimeObjectPlan(obj.Properties.Select(PlanProperty).ToList()),
        ExecutionCallExpression call => new RuntimeCallPlan(PlanExpression(call.Callee), call.Arguments.Select(PlanExpression).ToList()),
        ExecutionMemberExpression member => new RuntimeMemberPlan(PlanExpression(member.Target), member.Member),
        ExecutionIndexExpression index => new RuntimeIndexPlan(PlanExpression(index.Target), PlanExpression(index.Index)),
        ExecutionUnaryExpression unary => new RuntimeUnaryPlan(unary.Operator, PlanExpression(unary.Operand)),
        ExecutionBinaryExpression binary => new RuntimeBinaryPlan(PlanExpression(binary.Left), binary.Operator, PlanExpression(binary.Right)),
        ExecutionParenthesizedExpression paren => new RuntimeParenthesizedPlan(PlanExpression(paren.Expression)),
        _ => throw new InvalidOperationException($"Unsupported execution expression: {expression.GetType().Name}")
    };
}
