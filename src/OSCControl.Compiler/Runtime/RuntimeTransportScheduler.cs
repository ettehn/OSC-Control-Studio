namespace OSCControl.Compiler.Runtime;

internal sealed class RuntimeTransportScheduler
{
    private readonly IReadOnlyDictionary<string, RuntimeResolvedEndpoint> _endpoints;
    private readonly RuntimeExpressionEvaluator _evaluator;
    private readonly IRuntimeTransportDispatcher _dispatcher;

    public RuntimeTransportScheduler(
        IReadOnlyDictionary<string, RuntimeEndpointPlan> endpoints,
        RuntimeExpressionEvaluator evaluator,
        IRuntimeTransportDispatcher dispatcher)
    {
        _evaluator = evaluator;
        _dispatcher = dispatcher;
        _endpoints = RuntimeEndpointResolver.Resolve(endpoints);
    }

    public Task DispatchAsync(RuntimeTransportSendPlan step, RuntimeExecutionScope scope, CancellationToken cancellationToken)
    {
        if (!_endpoints.TryGetValue(step.TargetEndpoint, out var endpoint))
        {
            throw new InvalidOperationException($"Unknown endpoint '{step.TargetEndpoint}'.");
        }

        scope.CurrentTargetEndpoint = step.TargetEndpoint;
        try
        {
            var message = new RuntimeOutboundMessage(
                step.TargetEndpoint,
                EvaluateOptionalString(step.Message.Address, scope),
                EvaluateArgs(step.Message.Args, scope),
                EvaluateOptionalValue(step.Message.Body, scope),
                EvaluateOptionalObject(step.Message.Headers, scope),
                EvaluateExtras(step.Message.Extras, scope));

            return _dispatcher.DispatchAsync(endpoint, message, cancellationToken);
        }
        finally
        {
            scope.CurrentTargetEndpoint = null;
        }
    }

    private string? EvaluateOptionalString(RuntimeExpressionPlan? expression, RuntimeExecutionScope scope)
    {
        if (expression is null)
        {
            return null;
        }

        var value = _evaluator.Evaluate(expression, scope);
        return value is null ? null : RuntimeValueHelpers.ToStringValue(value);
    }

    private object? EvaluateOptionalValue(RuntimeExpressionPlan? expression, RuntimeExecutionScope scope) =>
        expression is null ? null : RuntimeValueHelpers.CloneValue(_evaluator.Evaluate(expression, scope));

    private Dictionary<string, object?> EvaluateOptionalObject(RuntimeExpressionPlan? expression, RuntimeExecutionScope scope) =>
        expression is null ? new Dictionary<string, object?>(StringComparer.Ordinal) : RuntimeValueHelpers.NormalizeObject(_evaluator.Evaluate(expression, scope));

    private List<object?> EvaluateArgs(RuntimeExpressionPlan? expression, RuntimeExecutionScope scope) =>
        expression is null ? [] : RuntimeValueHelpers.NormalizeArgs(_evaluator.Evaluate(expression, scope));

    private Dictionary<string, object?> EvaluateExtras(IReadOnlyList<RuntimePropertyPlan> properties, RuntimeExecutionScope scope)
    {
        var extras = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var property in properties)
        {
            extras[property.Key] = RuntimeValueHelpers.CloneValue(_evaluator.Evaluate(property.Value, scope));
        }

        return extras;
    }
}
