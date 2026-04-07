namespace OSCControl.Compiler.Runtime;

public sealed class RuntimeEngine : IAsyncDisposable
{
    private readonly RuntimePlan _plan;
    private readonly Dictionary<string, RuntimeEndpointPlan> _endpoints;
    private readonly RuntimeExpressionEvaluator _evaluator;
    private readonly RuntimeStepExecutor _stepExecutor;
    private readonly SemaphoreSlim _dispatchGate = new(1, 1);
    private bool _disposed;

    public RuntimeEngine(RuntimePlan plan, RuntimeEngineOptions? options = null)
    {
        _plan = plan;
        options ??= new RuntimeEngineOptions();

        Clock = options.Clock ?? new SystemRuntimeClock();
        TransportDispatcher = options.TransportDispatcher ?? new NetworkTransportDispatcher();
        LogSink = options.LogSink ?? new RecordingRuntimeLogSink();
        CommandInvoker = options.CommandInvoker ?? new DefaultRuntimeCommandInvoker();
        State = new RuntimeStateStore();

        _endpoints = plan.Endpoints.ToDictionary(endpoint => endpoint.Name, StringComparer.Ordinal);
        _evaluator = new RuntimeExpressionEvaluator();
        var transportScheduler = new RuntimeTransportScheduler(_endpoints, _evaluator, TransportDispatcher);
        _stepExecutor = new RuntimeStepExecutor(_evaluator, transportScheduler, LogSink, CommandInvoker);

        InitializeState();
    }

    public IRuntimeClock Clock { get; }
    public IRuntimeTransportDispatcher TransportDispatcher { get; }
    public IRuntimeLogSink LogSink { get; }
    public IRuntimeCommandInvoker CommandInvoker { get; }
    public RuntimeStateStore State { get; }
    public IReadOnlyDictionary<string, RuntimeEndpointPlan> Endpoints => _endpoints;

    public Task<RuntimeDispatchResult> StartAsync(CancellationToken cancellationToken = default) =>
        DispatchSerializedAsync(static rule => rule.Trigger is RuntimeStartupTriggerPlan, null, cancellationToken);

    public Task<RuntimeDispatchResult> ReceiveAsync(string endpointName, RuntimeEventMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        message.SourceEndpoint = endpointName;

        return DispatchSerializedAsync(
            rule => rule.Trigger switch
            {
                RuntimeReceiveTriggerPlan receive => receive.EndpointName == endpointName,
                RuntimeAddressTriggerPlan address => string.Equals(address.Address, message.Address, StringComparison.Ordinal),
                _ => false
            },
            message,
            cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        switch (TransportDispatcher)
        {
            case IAsyncDisposable asyncDisposable:
                await asyncDisposable.DisposeAsync();
                break;
            case IDisposable disposable:
                disposable.Dispose();
                break;
        }

        _dispatchGate.Dispose();
    }

    private void InitializeState()
    {
        var scope = new RuntimeExecutionScope(State, null, Clock);
        foreach (var state in _plan.States)
        {
            State.Store(state.Name, _evaluator.Evaluate(state.InitialValue, scope));
        }
    }

    private async Task<RuntimeDispatchResult> DispatchSerializedAsync(Func<RuntimeRulePlan, bool> predicate, RuntimeEventMessage? message, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        await _dispatchGate.WaitAsync(cancellationToken);
        try
        {
            return await DispatchCoreAsync(predicate, message, cancellationToken);
        }
        finally
        {
            _dispatchGate.Release();
        }
    }

    private async Task<RuntimeDispatchResult> DispatchCoreAsync(Func<RuntimeRulePlan, bool> predicate, RuntimeEventMessage? message, CancellationToken cancellationToken)
    {
        var matchedRules = 0;
        var stopRequested = false;
        var sharedMessage = message?.Clone();

        foreach (var rule in _plan.Rules.OrderBy(rule => rule.Order))
        {
            if (!predicate(rule))
            {
                continue;
            }

            var scope = new RuntimeExecutionScope(State, sharedMessage, Clock);
            if (rule.Guard is not null && !RuntimeValueHelpers.IsTruthy(_evaluator.Evaluate(rule.Guard, scope)))
            {
                continue;
            }

            matchedRules++;
            await _stepExecutor.ExecuteAsync(rule.Steps, scope, cancellationToken);
            if (scope.StopRequested)
            {
                stopRequested = true;
                break;
            }
        }

        return new RuntimeDispatchResult(matchedRules, stopRequested);
    }
}
