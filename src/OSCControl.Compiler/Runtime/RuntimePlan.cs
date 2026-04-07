namespace OSCControl.Compiler.Runtime;

public sealed record RuntimePlan(
    IReadOnlyList<RuntimeEndpointPlan> Endpoints,
    IReadOnlyList<RuntimeStatePlan> States,
    IReadOnlyList<RuntimeRulePlan> Rules);

public sealed record RuntimeEndpointPlan(
    string Name,
    string TransportKind,
    IReadOnlyList<RuntimePropertyPlan> Config);

public sealed record RuntimeStatePlan(
    string Name,
    RuntimeExpressionPlan InitialValue);

public sealed record RuntimeRulePlan(
    int Order,
    RuntimeTriggerPlan Trigger,
    RuntimeExpressionPlan? Guard,
    IReadOnlyList<RuntimeStepPlan> Steps);

public abstract record RuntimeTriggerPlan;

public sealed record RuntimeReceiveTriggerPlan(string EndpointName) : RuntimeTriggerPlan;
public sealed record RuntimeAddressTriggerPlan(string Address) : RuntimeTriggerPlan;
public sealed record RuntimeTimerTriggerPlan(double Interval) : RuntimeTriggerPlan;
public sealed record RuntimeStartupTriggerPlan() : RuntimeTriggerPlan;

public abstract record RuntimeStepPlan;

public sealed record RuntimeTransportSendPlan(
    string TargetEndpoint,
    RuntimeMessagePlan Message) : RuntimeStepPlan;

public sealed record RuntimeStateStorePlan(
    string StateName,
    RuntimeExpressionPlan Value) : RuntimeStepPlan;

public sealed record RuntimeAssignPlan(
    RuntimeExpressionPlan Target,
    RuntimeExpressionPlan Value) : RuntimeStepPlan;

public sealed record RuntimeLogPlan(
    string Level,
    RuntimeExpressionPlan Value) : RuntimeStepPlan;

public sealed record RuntimeBranchPlan(
    RuntimeExpressionPlan Condition,
    IReadOnlyList<RuntimeStepPlan> ThenSteps,
    IReadOnlyList<RuntimeStepPlan>? ElseSteps) : RuntimeStepPlan;

public sealed record RuntimeForEachPlan(
    string IteratorName,
    RuntimeExpressionPlan Source,
    IReadOnlyList<RuntimeStepPlan> Body) : RuntimeStepPlan;

public sealed record RuntimeWhilePlan(
    RuntimeExpressionPlan Condition,
    IReadOnlyList<RuntimeStepPlan> Body) : RuntimeStepPlan;

public sealed record RuntimeBreakPlan() : RuntimeStepPlan;

public sealed record RuntimeContinuePlan() : RuntimeStepPlan;

public sealed record RuntimeInvokePlan(
    string Name,
    IReadOnlyList<RuntimeExpressionPlan> Arguments) : RuntimeStepPlan;

public sealed record RuntimeStopPlan() : RuntimeStepPlan;

public sealed record RuntimeLetPlan(
    string Name,
    RuntimeExpressionPlan Value) : RuntimeStepPlan;

public sealed record RuntimeMessagePlan(
    RuntimeExpressionPlan? Address,
    RuntimeExpressionPlan? Args,
    RuntimeExpressionPlan? Body,
    RuntimeExpressionPlan? Headers,
    IReadOnlyList<RuntimePropertyPlan> Extras);

public sealed record RuntimePropertyPlan(string Key, RuntimeExpressionPlan Value);

public abstract record RuntimeExpressionPlan;

public sealed record RuntimeIdentifierPlan(string Name) : RuntimeExpressionPlan;
public sealed record RuntimeNumberPlan(double Value) : RuntimeExpressionPlan;
public sealed record RuntimeStringPlan(string Value) : RuntimeExpressionPlan;
public sealed record RuntimeBooleanPlan(bool Value) : RuntimeExpressionPlan;
public sealed record RuntimeNullPlan() : RuntimeExpressionPlan;
public sealed record RuntimeListPlan(IReadOnlyList<RuntimeExpressionPlan> Items) : RuntimeExpressionPlan;
public sealed record RuntimeObjectPlan(IReadOnlyList<RuntimePropertyPlan> Properties) : RuntimeExpressionPlan;
public sealed record RuntimeCallPlan(RuntimeExpressionPlan Callee, IReadOnlyList<RuntimeExpressionPlan> Arguments) : RuntimeExpressionPlan;
public sealed record RuntimeMemberPlan(RuntimeExpressionPlan Target, string Member) : RuntimeExpressionPlan;
public sealed record RuntimeIndexPlan(RuntimeExpressionPlan Target, RuntimeExpressionPlan Index) : RuntimeExpressionPlan;
public sealed record RuntimeUnaryPlan(string Operator, RuntimeExpressionPlan Operand) : RuntimeExpressionPlan;
public sealed record RuntimeBinaryPlan(RuntimeExpressionPlan Left, string Operator, RuntimeExpressionPlan Right) : RuntimeExpressionPlan;
public sealed record RuntimeParenthesizedPlan(RuntimeExpressionPlan Expression) : RuntimeExpressionPlan;
