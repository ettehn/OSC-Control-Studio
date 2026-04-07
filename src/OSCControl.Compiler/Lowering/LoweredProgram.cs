namespace OSCControl.Compiler.Lowering;

public sealed record LoweredProgram(
    IReadOnlyList<LoweredEndpoint> Endpoints,
    IReadOnlyList<LoweredState> States,
    IReadOnlyList<LoweredRule> Rules);

public sealed record LoweredEndpoint(
    string Name,
    string EndpointType,
    IReadOnlyList<LoweredProperty> Config);

public sealed record LoweredState(
    string Name,
    LoweredExpression InitialValue);

public sealed record LoweredRule(
    LoweredTrigger Trigger,
    LoweredExpression? Condition,
    IReadOnlyList<LoweredStep> Steps);

public abstract record LoweredTrigger;

public sealed record LoweredReceiveTrigger(string EndpointName) : LoweredTrigger;
public sealed record LoweredAddressTrigger(string Address) : LoweredTrigger;
public sealed record LoweredTimerTrigger(double Interval) : LoweredTrigger;
public sealed record LoweredStartupTrigger() : LoweredTrigger;

public abstract record LoweredStep;

public sealed record LoweredSendStep(
    string Target,
    IReadOnlyList<LoweredProperty> Payload) : LoweredStep;

public sealed record LoweredSetStep(
    LoweredExpression Target,
    LoweredExpression Value) : LoweredStep;

public sealed record LoweredStoreStep(
    string Name,
    LoweredExpression Value) : LoweredStep;

public sealed record LoweredLogStep(
    string? Level,
    LoweredExpression Value) : LoweredStep;

public sealed record LoweredCallStep(
    string Name,
    IReadOnlyList<LoweredExpression> Arguments) : LoweredStep;

public sealed record LoweredStopStep() : LoweredStep;

public sealed record LoweredLetStep(
    string Name,
    LoweredExpression Value) : LoweredStep;

public sealed record LoweredIfStep(
    LoweredExpression Condition,
    IReadOnlyList<LoweredStep> ThenSteps,
    IReadOnlyList<LoweredStep>? ElseSteps) : LoweredStep;

public sealed record LoweredForEachStep(
    string IteratorName,
    LoweredExpression Source,
    IReadOnlyList<LoweredStep> Body) : LoweredStep;

public sealed record LoweredWhileStep(
    LoweredExpression Condition,
    IReadOnlyList<LoweredStep> Body) : LoweredStep;

public sealed record LoweredBreakStep() : LoweredStep;

public sealed record LoweredContinueStep() : LoweredStep;

public abstract record LoweredExpression;

public sealed record LoweredIdentifierExpression(string Name) : LoweredExpression;
public sealed record LoweredNumberExpression(double Value) : LoweredExpression;
public sealed record LoweredStringExpression(string Value) : LoweredExpression;
public sealed record LoweredBooleanExpression(bool Value) : LoweredExpression;
public sealed record LoweredNullExpression() : LoweredExpression;
public sealed record LoweredListExpression(IReadOnlyList<LoweredExpression> Items) : LoweredExpression;
public sealed record LoweredObjectExpression(IReadOnlyList<LoweredProperty> Properties) : LoweredExpression;
public sealed record LoweredCallExpression(LoweredExpression Callee, IReadOnlyList<LoweredExpression> Arguments) : LoweredExpression;
public sealed record LoweredMemberExpression(LoweredExpression Target, string Member) : LoweredExpression;
public sealed record LoweredIndexExpression(LoweredExpression Target, LoweredExpression Index) : LoweredExpression;
public sealed record LoweredUnaryExpression(string Operator, LoweredExpression Operand) : LoweredExpression;
public sealed record LoweredBinaryExpression(LoweredExpression Left, string Operator, LoweredExpression Right) : LoweredExpression;
public sealed record LoweredParenthesizedExpression(LoweredExpression Expression) : LoweredExpression;

public sealed record LoweredProperty(string Key, LoweredExpression Value);
