using System.Collections;
using System.Globalization;

namespace OSCControl.Compiler.Runtime;

internal sealed class RuntimeExpressionEvaluator
{
    public object? Evaluate(RuntimeExpressionPlan expression, RuntimeExecutionScope scope, bool allowIdentifierFallback = false) => expression switch
    {
        RuntimeIdentifierPlan identifier => EvaluateIdentifier(identifier, scope, allowIdentifierFallback),
        RuntimeNumberPlan number => number.Value,
        RuntimeStringPlan str => str.Value,
        RuntimeBooleanPlan boolean => boolean.Value,
        RuntimeNullPlan => null,
        RuntimeListPlan list => list.Items.Select(item => RuntimeValueHelpers.CloneValue(Evaluate(item, scope))).ToList(),
        RuntimeObjectPlan obj => obj.Properties.ToDictionary(property => property.Key, property => RuntimeValueHelpers.CloneValue(Evaluate(property.Value, scope)), StringComparer.Ordinal),
        RuntimeCallPlan call => EvaluateCall(call, scope),
        RuntimeMemberPlan member => RuntimeValueHelpers.GetMember(Evaluate(member.Target, scope), member.Member),
        RuntimeIndexPlan index => RuntimeValueHelpers.GetIndex(Evaluate(index.Target, scope), Evaluate(index.Index, scope)),
        RuntimeUnaryPlan unary => EvaluateUnary(unary, scope),
        RuntimeBinaryPlan binary => EvaluateBinary(binary, scope),
        RuntimeParenthesizedPlan paren => Evaluate(paren.Expression, scope, allowIdentifierFallback),
        _ => throw new InvalidOperationException($"Unsupported runtime expression '{expression.GetType().Name}'.")
    };

    private static object? EvaluateIdentifier(RuntimeIdentifierPlan identifier, RuntimeExecutionScope scope, bool allowIdentifierFallback)
    {
        if (identifier.Name == "msg")
        {
            return scope.Message;
        }

        if (scope.Locals.TryGetValue(identifier.Name, out var local))
        {
            return RuntimeValueHelpers.CloneValue(local);
        }

        if (scope.State.Contains(identifier.Name))
        {
            return scope.State.Load(identifier.Name);
        }

        return allowIdentifierFallback ? identifier.Name : null;
    }

    private object? EvaluateCall(RuntimeCallPlan call, RuntimeExecutionScope scope)
    {
        var name = call.Callee switch
        {
            RuntimeIdentifierPlan identifier => identifier.Name,
            _ => RuntimeValueHelpers.ToStringValue(Evaluate(call.Callee, scope, true))
        };

        var arguments = call.Arguments.Select(argument => Evaluate(argument, scope)).ToArray();
        return name switch
        {
            "arg" => EvaluateArg(arguments, scope),
            "body" => EvaluateBody(arguments, scope),
            "header" => EvaluateHeader(arguments, scope),
            "state" => EvaluateState(arguments, scope),
            "source" => scope.Message?.SourceEndpoint,
            "target" => scope.CurrentTargetEndpoint,
            "address" => scope.Message?.Address,
            "count" => RuntimeValueHelpers.ToCount(arguments.FirstOrDefault()),
            "int" => Convert.ToInt32(RuntimeValueHelpers.ToNumber(arguments.FirstOrDefault()), CultureInfo.InvariantCulture),
            "float" => RuntimeValueHelpers.ToNumber(arguments.FirstOrDefault()),
            "string" => RuntimeValueHelpers.ToStringValue(arguments.FirstOrDefault()),
            "bool" => RuntimeValueHelpers.ToBoolean(arguments.FirstOrDefault()),
            "json" => RuntimeValueHelpers.ToJson(arguments.FirstOrDefault()),
            "concat" => string.Concat(arguments.Select(RuntimeValueHelpers.ToStringValue)),
            "contains" => RuntimeValueHelpers.ToStringValue(arguments.ElementAtOrDefault(0)).Contains(RuntimeValueHelpers.ToStringValue(arguments.ElementAtOrDefault(1)), StringComparison.Ordinal),
            "startsWith" => RuntimeValueHelpers.ToStringValue(arguments.ElementAtOrDefault(0)).StartsWith(RuntimeValueHelpers.ToStringValue(arguments.ElementAtOrDefault(1)), StringComparison.Ordinal),
            "endsWith" => RuntimeValueHelpers.ToStringValue(arguments.ElementAtOrDefault(0)).EndsWith(RuntimeValueHelpers.ToStringValue(arguments.ElementAtOrDefault(1)), StringComparison.Ordinal),
            "replace" => RuntimeValueHelpers.ToStringValue(arguments.ElementAtOrDefault(0)).Replace(RuntimeValueHelpers.ToStringValue(arguments.ElementAtOrDefault(1)), RuntimeValueHelpers.ToStringValue(arguments.ElementAtOrDefault(2)), StringComparison.Ordinal),
            "min" => Math.Min(RuntimeValueHelpers.ToNumber(arguments.ElementAtOrDefault(0)), RuntimeValueHelpers.ToNumber(arguments.ElementAtOrDefault(1))),
            "max" => Math.Max(RuntimeValueHelpers.ToNumber(arguments.ElementAtOrDefault(0)), RuntimeValueHelpers.ToNumber(arguments.ElementAtOrDefault(1))),
            "clamp" => Math.Clamp(RuntimeValueHelpers.ToNumber(arguments.ElementAtOrDefault(0)), RuntimeValueHelpers.ToNumber(arguments.ElementAtOrDefault(1)), RuntimeValueHelpers.ToNumber(arguments.ElementAtOrDefault(2))),
            "round" => Math.Round(RuntimeValueHelpers.ToNumber(arguments.ElementAtOrDefault(0))),
            "range" => EvaluateRange(arguments),
            "now" => scope.Clock.UtcNow.ToString("O", CultureInfo.InvariantCulture),
            "timestamp" => scope.Clock.UtcNow.ToUnixTimeMilliseconds(),
            "exists" => scope.State.Contains(RuntimeValueHelpers.ToStringValue(arguments.FirstOrDefault())),
            "env" => RuntimeEnvironmentProbe.Evaluate(arguments, scope.Clock),
            _ => throw new InvalidOperationException($"Unknown runtime function '{name}'.")
        };
    }

    private static object? EvaluateArg(object?[] arguments, RuntimeExecutionScope scope)
    {
        var index = Convert.ToInt32(RuntimeValueHelpers.ToNumber(arguments.FirstOrDefault()), CultureInfo.InvariantCulture);
        return scope.Message is not null && index >= 0 && index < scope.Message.Args.Count
            ? RuntimeValueHelpers.CloneValue(scope.Message.Args[index])
            : null;
    }

    private static object? EvaluateBody(object?[] arguments, RuntimeExecutionScope scope)
    {
        if (scope.Message is null)
        {
            return null;
        }

        var path = arguments.Length == 0 ? null : RuntimeValueHelpers.ToStringValue(arguments[0]);
        return RuntimeValueHelpers.CloneValue(RuntimeValueHelpers.GetPathValue(scope.Message.Body, path));
    }

    private static object? EvaluateHeader(object?[] arguments, RuntimeExecutionScope scope)
    {
        if (scope.Message is null || arguments.Length == 0)
        {
            return null;
        }

        return scope.Message.Headers.TryGetValue(RuntimeValueHelpers.ToStringValue(arguments[0]), out var value)
            ? RuntimeValueHelpers.CloneValue(value)
            : null;
    }

    private static object? EvaluateState(object?[] arguments, RuntimeExecutionScope scope)
    {
        if (arguments.Length == 0)
        {
            return null;
        }

        return scope.State.Load(RuntimeValueHelpers.ToStringValue(arguments[0]));
    }

    private static List<object?> EvaluateRange(object?[] arguments)
    {
        if (arguments.Length is < 1 or > 3)
        {
            throw new InvalidOperationException("range expects one, two, or three arguments.");
        }

        double start;
        double end;
        double step;

        if (arguments.Length == 1)
        {
            start = 0;
            end = RuntimeValueHelpers.ToNumber(arguments[0]);
            step = 1;
        }
        else
        {
            start = RuntimeValueHelpers.ToNumber(arguments[0]);
            end = RuntimeValueHelpers.ToNumber(arguments[1]);
            step = arguments.Length == 3 ? RuntimeValueHelpers.ToNumber(arguments[2]) : 1;
        }

        if (step == 0)
        {
            throw new InvalidOperationException("range step cannot be 0.");
        }

        var values = new List<object?>();
        if (step > 0)
        {
            for (var value = start; value < end; value += step)
            {
                values.Add(value);
            }
        }
        else
        {
            for (var value = start; value > end; value += step)
            {
                values.Add(value);
            }
        }

        return values;
    }

    private object? EvaluateUnary(RuntimeUnaryPlan unary, RuntimeExecutionScope scope)
    {
        var operand = Evaluate(unary.Operand, scope);
        return unary.Operator switch
        {
            "not" => !RuntimeValueHelpers.IsTruthy(operand),
            "-" => -RuntimeValueHelpers.ToNumber(operand),
            _ => throw new InvalidOperationException($"Unsupported unary operator '{unary.Operator}'.")
        };
    }

    private object? EvaluateBinary(RuntimeBinaryPlan binary, RuntimeExecutionScope scope)
    {
        if (binary.Operator == "and")
        {
            var left = Evaluate(binary.Left, scope);
            return RuntimeValueHelpers.IsTruthy(left) && RuntimeValueHelpers.IsTruthy(Evaluate(binary.Right, scope));
        }

        if (binary.Operator == "or")
        {
            var left = Evaluate(binary.Left, scope);
            return RuntimeValueHelpers.IsTruthy(left) || RuntimeValueHelpers.IsTruthy(Evaluate(binary.Right, scope));
        }

        var leftValue = Evaluate(binary.Left, scope);
        var rightValue = Evaluate(binary.Right, scope);

        return binary.Operator switch
        {
            "==" => RuntimeValueHelpers.ValuesEqual(leftValue, rightValue),
            "!=" => !RuntimeValueHelpers.ValuesEqual(leftValue, rightValue),
            ">" => RuntimeValueHelpers.ToNumber(leftValue) > RuntimeValueHelpers.ToNumber(rightValue),
            ">=" => RuntimeValueHelpers.ToNumber(leftValue) >= RuntimeValueHelpers.ToNumber(rightValue),
            "<" => RuntimeValueHelpers.ToNumber(leftValue) < RuntimeValueHelpers.ToNumber(rightValue),
            "<=" => RuntimeValueHelpers.ToNumber(leftValue) <= RuntimeValueHelpers.ToNumber(rightValue),
            "+" => leftValue is string || rightValue is string
                ? RuntimeValueHelpers.ToStringValue(leftValue) + RuntimeValueHelpers.ToStringValue(rightValue)
                : RuntimeValueHelpers.ToNumber(leftValue) + RuntimeValueHelpers.ToNumber(rightValue),
            "-" => RuntimeValueHelpers.ToNumber(leftValue) - RuntimeValueHelpers.ToNumber(rightValue),
            "*" => RuntimeValueHelpers.ToNumber(leftValue) * RuntimeValueHelpers.ToNumber(rightValue),
            "/" => RuntimeValueHelpers.ToNumber(leftValue) / RuntimeValueHelpers.ToNumber(rightValue),
            "%" => RuntimeValueHelpers.ToNumber(leftValue) % RuntimeValueHelpers.ToNumber(rightValue),
            _ => throw new InvalidOperationException($"Unsupported binary operator '{binary.Operator}'.")
        };
    }
}
