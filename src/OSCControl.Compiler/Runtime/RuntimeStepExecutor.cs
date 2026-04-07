using System.Collections;
using System.Collections.ObjectModel;

namespace OSCControl.Compiler.Runtime;

internal sealed class RuntimeStepExecutor
{
    private readonly RuntimeExpressionEvaluator _evaluator;
    private readonly RuntimeTransportScheduler _transportScheduler;
    private readonly IRuntimeLogSink _logSink;
    private readonly IRuntimeCommandInvoker _commandInvoker;
    private readonly IReadOnlyDictionary<string, RuntimeFunctionPlan> _functions;

    public RuntimeStepExecutor(
        RuntimeExpressionEvaluator evaluator,
        RuntimeTransportScheduler transportScheduler,
        IRuntimeLogSink logSink,
        IRuntimeCommandInvoker commandInvoker,
        IReadOnlyDictionary<string, RuntimeFunctionPlan> functions)
    {
        _evaluator = evaluator;
        _transportScheduler = transportScheduler;
        _logSink = logSink;
        _commandInvoker = commandInvoker;
        _functions = functions;
    }

    public async Task ExecuteAsync(IReadOnlyList<RuntimeStepPlan> steps, RuntimeExecutionScope scope, CancellationToken cancellationToken)
    {
        foreach (var step in steps)
        {
            if (scope.StopRequested || scope.BreakRequested || scope.ContinueRequested)
            {
                return;
            }

            cancellationToken.ThrowIfCancellationRequested();
            await ExecuteStepAsync(step, scope, cancellationToken);
        }
    }

    private async Task ExecuteStepAsync(RuntimeStepPlan step, RuntimeExecutionScope scope, CancellationToken cancellationToken)
    {
        switch (step)
        {
            case RuntimeTransportSendPlan send:
                await _transportScheduler.DispatchAsync(send, scope, cancellationToken);
                return;

            case RuntimeStateStorePlan store:
                scope.State.Store(store.StateName, _evaluator.Evaluate(store.Value, scope));
                return;

            case RuntimeAssignPlan assign:
                Assign(assign.Target, _evaluator.Evaluate(assign.Value, scope), scope);
                return;

            case RuntimeLogPlan log:
                _logSink.Write(new RuntimeLogEntry(scope.Clock.UtcNow, log.Level, RuntimeValueHelpers.CloneValue(_evaluator.Evaluate(log.Value, scope))));
                return;

            case RuntimeBranchPlan branch:
                var branchResult = RuntimeValueHelpers.IsTruthy(_evaluator.Evaluate(branch.Condition, scope));
                await ExecuteAsync(branchResult ? branch.ThenSteps : branch.ElseSteps ?? [], scope, cancellationToken);
                return;

            case RuntimeForEachPlan loop:
                await ExecuteForEachAsync(loop, scope, cancellationToken);
                return;

            case RuntimeWhilePlan loop:
                await ExecuteWhileAsync(loop, scope, cancellationToken);
                return;

            case RuntimeBreakPlan:
                scope.BreakRequested = true;
                return;

            case RuntimeContinuePlan:
                scope.ContinueRequested = true;
                return;

            case RuntimeInvokePlan invoke:
                var arguments = invoke.Arguments.Select(argument => RuntimeValueHelpers.CloneValue(_evaluator.Evaluate(argument, scope))).ToArray();
                if (_functions.TryGetValue(invoke.Name, out var function))
                {
                    await InvokeFunctionAsync(function, arguments, scope, cancellationToken);
                    return;
                }

                await _commandInvoker.InvokeAsync(invoke.Name, arguments, new RuntimeCommandContext(scope.State, scope.Message, new ReadOnlyDictionary<string, object?>(scope.Locals), scope.Clock), cancellationToken);
                return;

            case RuntimeStopPlan:
                scope.StopRequested = true;
                return;

            case RuntimeLetPlan let:
                scope.Locals[let.Name] = RuntimeValueHelpers.CloneValue(_evaluator.Evaluate(let.Value, scope));
                return;

            default:
                throw new InvalidOperationException($"Unsupported runtime step '{step.GetType().Name}'.");
        }
    }

    private async Task InvokeFunctionAsync(RuntimeFunctionPlan function, IReadOnlyList<object?> arguments, RuntimeExecutionScope scope, CancellationToken cancellationToken)
    {
        if (arguments.Count != function.Parameters.Count)
        {
            throw new InvalidOperationException($"Function '{function.Name}' expects {function.Parameters.Count} argument(s), got {arguments.Count}.");
        }

        var previousLocals = scope.Locals.ToDictionary(pair => pair.Key, pair => RuntimeValueHelpers.CloneValue(pair.Value), StringComparer.Ordinal);
        try
        {
            for (var index = 0; index < function.Parameters.Count; index++)
            {
                scope.Locals[function.Parameters[index]] = RuntimeValueHelpers.CloneValue(arguments[index]);
            }

            await ExecuteAsync(function.Steps, scope, cancellationToken);
        }
        finally
        {
            scope.Locals.Clear();
            foreach (var pair in previousLocals)
            {
                scope.Locals[pair.Key] = RuntimeValueHelpers.CloneValue(pair.Value);
            }
        }
    }
    private async Task ExecuteForEachAsync(RuntimeForEachPlan loop, RuntimeExecutionScope scope, CancellationToken cancellationToken)
    {
        var source = _evaluator.Evaluate(loop.Source, scope);
        var items = EnumerateItems(source);
        var hadPreviousValue = scope.Locals.TryGetValue(loop.IteratorName, out var previousValue);

        try
        {
            foreach (var item in items)
            {
                if (scope.StopRequested)
                {
                    return;
                }

                cancellationToken.ThrowIfCancellationRequested();
                scope.Locals[loop.IteratorName] = RuntimeValueHelpers.CloneValue(item);
                await ExecuteAsync(loop.Body, scope, cancellationToken);

                if (scope.StopRequested)
                {
                    return;
                }

                if (scope.BreakRequested)
                {
                    scope.BreakRequested = false;
                    break;
                }

                if (scope.ContinueRequested)
                {
                    scope.ContinueRequested = false;
                    continue;
                }
            }
        }
        finally
        {
            if (hadPreviousValue)
            {
                scope.Locals[loop.IteratorName] = previousValue;
            }
            else
            {
                scope.Locals.Remove(loop.IteratorName);
            }
        }
    }

    private async Task ExecuteWhileAsync(RuntimeWhilePlan loop, RuntimeExecutionScope scope, CancellationToken cancellationToken)
    {
        while (!scope.StopRequested && RuntimeValueHelpers.IsTruthy(_evaluator.Evaluate(loop.Condition, scope)))
        {
            cancellationToken.ThrowIfCancellationRequested();
            await ExecuteAsync(loop.Body, scope, cancellationToken);

            if (scope.StopRequested)
            {
                return;
            }

            if (scope.BreakRequested)
            {
                scope.BreakRequested = false;
                break;
            }

            if (scope.ContinueRequested)
            {
                scope.ContinueRequested = false;
            }
        }
    }

    private static IReadOnlyList<object?> EnumerateItems(object? source)
    {
        if (source is null)
        {
            return [];
        }

        if (source is string text)
        {
            return text.Select(character => (object?)character.ToString()).ToList();
        }

        if (source is IEnumerable enumerable)
        {
            var items = new List<object?>();
            foreach (var item in enumerable)
            {
                items.Add(RuntimeValueHelpers.CloneValue(item));
            }

            return items;
        }

        return [RuntimeValueHelpers.CloneValue(source)];
    }

    private void Assign(RuntimeExpressionPlan target, object? value, RuntimeExecutionScope scope)
    {
        switch (target)
        {
            case RuntimeIdentifierPlan identifier:
                if (scope.Locals.ContainsKey(identifier.Name))
                {
                    scope.Locals[identifier.Name] = RuntimeValueHelpers.CloneValue(value);
                }
                else if (scope.State.Contains(identifier.Name))
                {
                    scope.State.Store(identifier.Name, value);
                }
                else
                {
                    scope.Locals[identifier.Name] = RuntimeValueHelpers.CloneValue(value);
                }

                return;

            case RuntimeMemberPlan member:
                RuntimeValueHelpers.SetMember(_evaluator.Evaluate(member.Target, scope), member.Member, value);
                return;

            case RuntimeIndexPlan index:
                RuntimeValueHelpers.SetIndex(_evaluator.Evaluate(index.Target, scope), _evaluator.Evaluate(index.Index, scope), value);
                return;

            default:
                throw new InvalidOperationException($"Cannot assign to target '{target.GetType().Name}'.");
        }
    }
}
