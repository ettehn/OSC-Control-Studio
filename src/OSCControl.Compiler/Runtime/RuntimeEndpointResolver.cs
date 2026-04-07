namespace OSCControl.Compiler.Runtime;

internal static class RuntimeEndpointResolver
{
    public static IReadOnlyDictionary<string, RuntimeResolvedEndpoint> Resolve(IReadOnlyDictionary<string, RuntimeEndpointPlan> endpoints)
    {
        var evaluator = new RuntimeExpressionEvaluator();
        var scope = new RuntimeExecutionScope(new RuntimeStateStore(), null, new SystemRuntimeClock());
        return endpoints.ToDictionary(pair => pair.Key, pair => Resolve(pair.Value, evaluator, scope), StringComparer.Ordinal);
    }

    public static RuntimeResolvedEndpoint Resolve(RuntimeEndpointPlan endpoint)
    {
        var evaluator = new RuntimeExpressionEvaluator();
        var scope = new RuntimeExecutionScope(new RuntimeStateStore(), null, new SystemRuntimeClock());
        return Resolve(endpoint, evaluator, scope);
    }

    private static RuntimeResolvedEndpoint Resolve(RuntimeEndpointPlan endpoint, RuntimeExpressionEvaluator evaluator, RuntimeExecutionScope scope)
    {
        var config = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var property in endpoint.Config)
        {
            config[property.Key] = RuntimeValueHelpers.CloneValue(evaluator.Evaluate(property.Value, scope, allowIdentifierFallback: true));
        }

        return new RuntimeResolvedEndpoint(endpoint.Name, endpoint.TransportKind, config);
    }
}
