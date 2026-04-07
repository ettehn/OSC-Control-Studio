namespace OSCControl.Compiler.Execution;

public sealed record ExecutionProgram(
    IReadOnlyList<ExecutionEndpoint> Endpoints,
    IReadOnlyList<ExecutionStateSlot> States,
    IReadOnlyList<ExecutionRule> Rules);

public sealed record ExecutionEndpoint(
    string Name,
    string TransportKind,
    IReadOnlyList<ExecutionProperty> Config);

public sealed record ExecutionStateSlot(
    string Name,
    ExecutionExpression InitialValue);

public sealed record ExecutionRule(
    ExecutionTrigger Trigger,
    ExecutionExpression? Condition,
    IReadOnlyList<ExecutionStep> Steps);

public abstract record ExecutionTrigger;

public sealed record ExecutionReceiveTrigger(string EndpointName) : ExecutionTrigger;
public sealed record ExecutionAddressTrigger(string Address) : ExecutionTrigger;
public sealed record ExecutionTimerTrigger(double Interval) : ExecutionTrigger;
public sealed record ExecutionStartupTrigger() : ExecutionTrigger;

public abstract record ExecutionStep;

public sealed record ExecutionTransportSendStep(
    string TargetEndpoint,
    ExecutionMessageTemplate Message) : ExecutionStep;

public sealed record ExecutionStateStoreStep(
    string StateName,
    ExecutionExpression Value) : ExecutionStep;

public sealed record ExecutionAssignStep(
    ExecutionExpression Target,
    ExecutionExpression Value) : ExecutionStep;

public sealed record ExecutionLogStep(
    string Level,
    ExecutionExpression Value) : ExecutionStep;

public sealed record ExecutionBranchStep(
    ExecutionExpression Condition,
    IReadOnlyList<ExecutionStep> ThenSteps,
    IReadOnlyList<ExecutionStep>? ElseSteps) : ExecutionStep;

public sealed record ExecutionForEachStep(
    string IteratorName,
    ExecutionExpression Source,
    IReadOnlyList<ExecutionStep> Body) : ExecutionStep;

public sealed record ExecutionWhileStep(
    ExecutionExpression Condition,
    IReadOnlyList<ExecutionStep> Body) : ExecutionStep;

public sealed record ExecutionBreakStep() : ExecutionStep;

public sealed record ExecutionContinueStep() : ExecutionStep;

public sealed record ExecutionInvokeStep(
    string Name,
    IReadOnlyList<ExecutionExpression> Arguments) : ExecutionStep;

public sealed record ExecutionStopStep() : ExecutionStep;

public sealed record ExecutionLetStep(
    string Name,
    ExecutionExpression Value) : ExecutionStep;

public sealed record ExecutionMessageTemplate(
    ExecutionExpression? Address,
    ExecutionExpression? Args,
    ExecutionExpression? Body,
    ExecutionExpression? Headers,
    IReadOnlyList<ExecutionProperty> ExtraProperties);

public sealed record ExecutionProperty(string Key, ExecutionExpression Value);

public abstract record ExecutionExpression;

public sealed record ExecutionIdentifierExpression(string Name) : ExecutionExpression;
public sealed record ExecutionNumberExpression(double Value) : ExecutionExpression;
public sealed record ExecutionStringExpression(string Value) : ExecutionExpression;
public sealed record ExecutionBooleanExpression(bool Value) : ExecutionExpression;
public sealed record ExecutionNullExpression() : ExecutionExpression;
public sealed record ExecutionListExpression(IReadOnlyList<ExecutionExpression> Items) : ExecutionExpression;
public sealed record ExecutionObjectExpression(IReadOnlyList<ExecutionProperty> Properties) : ExecutionExpression;
public sealed record ExecutionCallExpression(ExecutionExpression Callee, IReadOnlyList<ExecutionExpression> Arguments) : ExecutionExpression;
public sealed record ExecutionMemberExpression(ExecutionExpression Target, string Member) : ExecutionExpression;
public sealed record ExecutionIndexExpression(ExecutionExpression Target, ExecutionExpression Index) : ExecutionExpression;
public sealed record ExecutionUnaryExpression(string Operator, ExecutionExpression Operand) : ExecutionExpression;
public sealed record ExecutionBinaryExpression(ExecutionExpression Left, string Operator, ExecutionExpression Right) : ExecutionExpression;
public sealed record ExecutionParenthesizedExpression(ExecutionExpression Expression) : ExecutionExpression;
