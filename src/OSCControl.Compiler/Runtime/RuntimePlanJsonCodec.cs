using System.Text.Json;
using System.Text.Json.Nodes;

namespace OSCControl.Compiler.Runtime;

public static class RuntimePlanJsonCodec
{
    private const string Format = "osccontrol.runtimePlan";
    private const int Version = 1;

    public static string Serialize(RuntimePlan plan, bool indented = true)
    {
        ArgumentNullException.ThrowIfNull(plan);

        var root = new JsonObject
        {
            ["format"] = Format,
            ["version"] = Version,
            ["endpoints"] = WriteArray(plan.Endpoints, WriteEndpoint),
            ["states"] = WriteArray(plan.States, WriteState),
            ["rules"] = WriteArray(plan.Rules, WriteRule),
            ["functions"] = WriteArray(plan.Functions, WriteFunction)
        };

        return root.ToJsonString(new JsonSerializerOptions { WriteIndented = indented, TypeInfoResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver() });
    }

    public static RuntimePlan Deserialize(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);

        var root = RequireObject(JsonNode.Parse(json), "root");
        var format = RequireString(root, "format");
        if (!string.Equals(format, Format, StringComparison.Ordinal))
        {
            throw new JsonException($"Unsupported runtime plan format '{format}'.");
        }

        var version = RequireInt32(root, "version");
        if (version != Version)
        {
            throw new JsonException($"Unsupported runtime plan version '{version}'.");
        }

        return new RuntimePlan(
            ReadArray(root, "endpoints", ReadEndpoint),
            ReadArray(root, "states", ReadState),
            ReadArray(root, "rules", ReadRule),
            ReadOptionalArray(root, "functions", ReadFunction) ?? []);
    }

    private static JsonNode WriteEndpoint(RuntimeEndpointPlan endpoint) => new JsonObject
    {
        ["name"] = endpoint.Name,
        ["transportKind"] = endpoint.TransportKind,
        ["config"] = WriteArray(endpoint.Config, WriteProperty)
    };

    private static RuntimeEndpointPlan ReadEndpoint(JsonNode node)
    {
        var obj = RequireObject(node, "endpoint");
        return new RuntimeEndpointPlan(
            RequireString(obj, "name"),
            RequireString(obj, "transportKind"),
            ReadArray(obj, "config", ReadProperty));
    }

    private static JsonNode WriteState(RuntimeStatePlan state) => new JsonObject
    {
        ["name"] = state.Name,
        ["initialValue"] = WriteExpression(state.InitialValue)
    };

    private static RuntimeStatePlan ReadState(JsonNode node)
    {
        var obj = RequireObject(node, "state");
        return new RuntimeStatePlan(
            RequireString(obj, "name"),
            ReadExpression(RequireNode(obj, "initialValue")));
    }

    private static JsonNode WriteRule(RuntimeRulePlan rule) => new JsonObject
    {
        ["order"] = rule.Order,
        ["trigger"] = WriteTrigger(rule.Trigger),
        ["guard"] = rule.Guard is null ? null : WriteExpression(rule.Guard),
        ["steps"] = WriteArray(rule.Steps, WriteStep)
    };

    private static RuntimeRulePlan ReadRule(JsonNode node)
    {
        var obj = RequireObject(node, "rule");
        return new RuntimeRulePlan(
            RequireInt32(obj, "order"),
            ReadTrigger(RequireNode(obj, "trigger")),
            ReadOptionalExpression(obj, "guard"),
            ReadArray(obj, "steps", ReadStep));
    }

    private static JsonNode WriteFunction(RuntimeFunctionPlan function) => new JsonObject
    {
        ["name"] = function.Name,
        ["parameters"] = WriteStringArray(function.Parameters),
        ["steps"] = WriteArray(function.Steps, WriteStep)
    };

    private static RuntimeFunctionPlan ReadFunction(JsonNode node)
    {
        var obj = RequireObject(node, "function");
        return new RuntimeFunctionPlan(
            RequireString(obj, "name"),
            ReadStringArray(obj, "parameters"),
            ReadArray(obj, "steps", ReadStep));
    }
    private static JsonNode WriteTrigger(RuntimeTriggerPlan trigger)
    {
        return trigger switch
        {
            RuntimeReceiveTriggerPlan receive => new JsonObject { ["kind"] = "receive", ["endpointName"] = receive.EndpointName },
            RuntimeAddressTriggerPlan address => new JsonObject { ["kind"] = "address", ["address"] = address.Address },
            RuntimeTimerTriggerPlan timer => new JsonObject { ["kind"] = "timer", ["interval"] = timer.Interval },
            RuntimeStartupTriggerPlan => new JsonObject { ["kind"] = "startup" },
            _ => throw new JsonException($"Unsupported runtime trigger type '{trigger.GetType().Name}'.")
        };
    }

    private static RuntimeTriggerPlan ReadTrigger(JsonNode node)
    {
        var obj = RequireObject(node, "trigger");
        return RequireString(obj, "kind") switch
        {
            "receive" => new RuntimeReceiveTriggerPlan(RequireString(obj, "endpointName")),
            "address" => new RuntimeAddressTriggerPlan(RequireString(obj, "address")),
            "timer" => new RuntimeTimerTriggerPlan(RequireDouble(obj, "interval")),
            "startup" => new RuntimeStartupTriggerPlan(),
            var kind => throw new JsonException($"Unsupported runtime trigger kind '{kind}'.")
        };
    }

    private static JsonNode WriteStep(RuntimeStepPlan step)
    {
        return step switch
        {
            RuntimeTransportSendPlan send => new JsonObject { ["kind"] = "send", ["targetEndpoint"] = send.TargetEndpoint, ["message"] = WriteMessage(send.Message) },
            RuntimeStateStorePlan store => new JsonObject { ["kind"] = "store", ["stateName"] = store.StateName, ["value"] = WriteExpression(store.Value) },
            RuntimeAssignPlan assign => new JsonObject { ["kind"] = "assign", ["target"] = WriteExpression(assign.Target), ["value"] = WriteExpression(assign.Value) },
            RuntimeLogPlan log => new JsonObject { ["kind"] = "log", ["level"] = log.Level, ["value"] = WriteExpression(log.Value) },
            RuntimeBranchPlan branch => new JsonObject { ["kind"] = "branch", ["condition"] = WriteExpression(branch.Condition), ["thenSteps"] = WriteArray(branch.ThenSteps, WriteStep), ["elseSteps"] = branch.ElseSteps is null ? null : WriteArray(branch.ElseSteps, WriteStep) },
            RuntimeForEachPlan forEach => new JsonObject { ["kind"] = "foreach", ["iteratorName"] = forEach.IteratorName, ["source"] = WriteExpression(forEach.Source), ["body"] = WriteArray(forEach.Body, WriteStep) },
            RuntimeWhilePlan loop => new JsonObject { ["kind"] = "while", ["condition"] = WriteExpression(loop.Condition), ["body"] = WriteArray(loop.Body, WriteStep) },
            RuntimeBreakPlan => new JsonObject { ["kind"] = "break" },
            RuntimeContinuePlan => new JsonObject { ["kind"] = "continue" },
            RuntimeInvokePlan invoke => new JsonObject { ["kind"] = "invoke", ["name"] = invoke.Name, ["arguments"] = WriteArray(invoke.Arguments, WriteExpression) },
            RuntimeStopPlan => new JsonObject { ["kind"] = "stop" },
            RuntimeLetPlan let => new JsonObject { ["kind"] = "let", ["name"] = let.Name, ["value"] = WriteExpression(let.Value) },
            _ => throw new JsonException($"Unsupported runtime step type '{step.GetType().Name}'.")
        };
    }

    private static RuntimeStepPlan ReadStep(JsonNode node)
    {
        var obj = RequireObject(node, "step");
        return RequireString(obj, "kind") switch
        {
            "send" => new RuntimeTransportSendPlan(RequireString(obj, "targetEndpoint"), ReadMessage(RequireNode(obj, "message"))),
            "store" => new RuntimeStateStorePlan(RequireString(obj, "stateName"), ReadExpression(RequireNode(obj, "value"))),
            "assign" => new RuntimeAssignPlan(ReadExpression(RequireNode(obj, "target")), ReadExpression(RequireNode(obj, "value"))),
            "log" => new RuntimeLogPlan(RequireString(obj, "level"), ReadExpression(RequireNode(obj, "value"))),
            "branch" => new RuntimeBranchPlan(ReadExpression(RequireNode(obj, "condition")), ReadArray(obj, "thenSteps", ReadStep), ReadOptionalArray(obj, "elseSteps", ReadStep)),
            "foreach" => new RuntimeForEachPlan(RequireString(obj, "iteratorName"), ReadExpression(RequireNode(obj, "source")), ReadArray(obj, "body", ReadStep)),
            "while" => new RuntimeWhilePlan(ReadExpression(RequireNode(obj, "condition")), ReadArray(obj, "body", ReadStep)),
            "break" => new RuntimeBreakPlan(),
            "continue" => new RuntimeContinuePlan(),
            "invoke" => new RuntimeInvokePlan(RequireString(obj, "name"), ReadArray(obj, "arguments", ReadExpression)),
            "stop" => new RuntimeStopPlan(),
            "let" => new RuntimeLetPlan(RequireString(obj, "name"), ReadExpression(RequireNode(obj, "value"))),
            var kind => throw new JsonException($"Unsupported runtime step kind '{kind}'.")
        };
    }

    private static JsonNode WriteMessage(RuntimeMessagePlan message) => new JsonObject
    {
        ["address"] = message.Address is null ? null : WriteExpression(message.Address),
        ["args"] = message.Args is null ? null : WriteExpression(message.Args),
        ["body"] = message.Body is null ? null : WriteExpression(message.Body),
        ["headers"] = message.Headers is null ? null : WriteExpression(message.Headers),
        ["extras"] = WriteArray(message.Extras, WriteProperty)
    };

    private static RuntimeMessagePlan ReadMessage(JsonNode node)
    {
        var obj = RequireObject(node, "message");
        return new RuntimeMessagePlan(
            ReadOptionalExpression(obj, "address"),
            ReadOptionalExpression(obj, "args"),
            ReadOptionalExpression(obj, "body"),
            ReadOptionalExpression(obj, "headers"),
            ReadArray(obj, "extras", ReadProperty));
    }

    private static JsonNode WriteProperty(RuntimePropertyPlan property) => new JsonObject
    {
        ["key"] = property.Key,
        ["value"] = WriteExpression(property.Value)
    };

    private static RuntimePropertyPlan ReadProperty(JsonNode node)
    {
        var obj = RequireObject(node, "property");
        return new RuntimePropertyPlan(RequireString(obj, "key"), ReadExpression(RequireNode(obj, "value")));
    }

    private static JsonNode WriteExpression(RuntimeExpressionPlan expression)
    {
        return expression switch
        {
            RuntimeIdentifierPlan identifier => new JsonObject { ["kind"] = "identifier", ["name"] = identifier.Name },
            RuntimeNumberPlan number => new JsonObject { ["kind"] = "number", ["value"] = number.Value },
            RuntimeStringPlan text => new JsonObject { ["kind"] = "string", ["value"] = text.Value },
            RuntimeBooleanPlan boolean => new JsonObject { ["kind"] = "boolean", ["value"] = boolean.Value },
            RuntimeNullPlan => new JsonObject { ["kind"] = "null" },
            RuntimeListPlan list => new JsonObject { ["kind"] = "list", ["items"] = WriteArray(list.Items, WriteExpression) },
            RuntimeObjectPlan obj => new JsonObject { ["kind"] = "object", ["properties"] = WriteArray(obj.Properties, WriteProperty) },
            RuntimeCallPlan call => new JsonObject { ["kind"] = "call", ["callee"] = WriteExpression(call.Callee), ["arguments"] = WriteArray(call.Arguments, WriteExpression) },
            RuntimeMemberPlan member => new JsonObject { ["kind"] = "member", ["target"] = WriteExpression(member.Target), ["member"] = member.Member },
            RuntimeIndexPlan index => new JsonObject { ["kind"] = "index", ["target"] = WriteExpression(index.Target), ["index"] = WriteExpression(index.Index) },
            RuntimeUnaryPlan unary => new JsonObject { ["kind"] = "unary", ["operator"] = unary.Operator, ["operand"] = WriteExpression(unary.Operand) },
            RuntimeBinaryPlan binary => new JsonObject { ["kind"] = "binary", ["left"] = WriteExpression(binary.Left), ["operator"] = binary.Operator, ["right"] = WriteExpression(binary.Right) },
            RuntimeParenthesizedPlan parenthesized => new JsonObject { ["kind"] = "parenthesized", ["expression"] = WriteExpression(parenthesized.Expression) },
            _ => throw new JsonException($"Unsupported runtime expression type '{expression.GetType().Name}'.")
        };
    }

    private static RuntimeExpressionPlan ReadExpression(JsonNode node)
    {
        var obj = RequireObject(node, "expression");
        return RequireString(obj, "kind") switch
        {
            "identifier" => new RuntimeIdentifierPlan(RequireString(obj, "name")),
            "number" => new RuntimeNumberPlan(RequireDouble(obj, "value")),
            "string" => new RuntimeStringPlan(RequireString(obj, "value")),
            "boolean" => new RuntimeBooleanPlan(RequireBoolean(obj, "value")),
            "null" => new RuntimeNullPlan(),
            "list" => new RuntimeListPlan(ReadArray(obj, "items", ReadExpression)),
            "object" => new RuntimeObjectPlan(ReadArray(obj, "properties", ReadProperty)),
            "call" => new RuntimeCallPlan(ReadExpression(RequireNode(obj, "callee")), ReadArray(obj, "arguments", ReadExpression)),
            "member" => new RuntimeMemberPlan(ReadExpression(RequireNode(obj, "target")), RequireString(obj, "member")),
            "index" => new RuntimeIndexPlan(ReadExpression(RequireNode(obj, "target")), ReadExpression(RequireNode(obj, "index"))),
            "unary" => new RuntimeUnaryPlan(RequireString(obj, "operator"), ReadExpression(RequireNode(obj, "operand"))),
            "binary" => new RuntimeBinaryPlan(ReadExpression(RequireNode(obj, "left")), RequireString(obj, "operator"), ReadExpression(RequireNode(obj, "right"))),
            "parenthesized" => new RuntimeParenthesizedPlan(ReadExpression(RequireNode(obj, "expression"))),
            var kind => throw new JsonException($"Unsupported runtime expression kind '{kind}'.")
        };
    }

    private static RuntimeExpressionPlan? ReadOptionalExpression(JsonObject obj, string propertyName)
    {
        return !obj.TryGetPropertyValue(propertyName, out var node) || node is null ? null : ReadExpression(node);
    }

    private static JsonArray WriteStringArray(IEnumerable<string> items)
    {
        var array = new JsonArray();
        foreach (var item in items)
        {
            array.Add(item);
        }

        return array;
    }

    private static IReadOnlyList<string> ReadStringArray(JsonObject obj, string propertyName)
    {
        var array = RequireArray(RequireNode(obj, propertyName), propertyName);
        return array.Select(node => node?.GetValue<string>() ?? throw new JsonException($"Array '{propertyName}' contains null.")).ToArray();
    }
    private static JsonArray WriteArray<T>(IEnumerable<T> items, Func<T, JsonNode> write)
    {
        var array = new JsonArray();
        foreach (var item in items)
        {
            array.Add(write(item));
        }

        return array;
    }

    private static IReadOnlyList<T> ReadArray<T>(JsonObject obj, string propertyName, Func<JsonNode, T> read)
    {
        var array = RequireArray(RequireNode(obj, propertyName), propertyName);
        return array.Select(node => read(node ?? throw new JsonException($"Array '{propertyName}' contains null."))).ToArray();
    }

    private static IReadOnlyList<T>? ReadOptionalArray<T>(JsonObject obj, string propertyName, Func<JsonNode, T> read)
    {
        if (!obj.TryGetPropertyValue(propertyName, out var node) || node is null)
        {
            return null;
        }

        var array = RequireArray(node, propertyName);
        return array.Select(item => read(item ?? throw new JsonException($"Array '{propertyName}' contains null."))).ToArray();
    }

    private static JsonNode RequireNode(JsonObject obj, string propertyName)
    {
        if (!obj.TryGetPropertyValue(propertyName, out var node) || node is null)
        {
            throw new JsonException($"Required property '{propertyName}' is missing.");
        }

        return node;
    }

    private static JsonObject RequireObject(JsonNode? node, string name) =>
        node as JsonObject ?? throw new JsonException($"Expected object for '{name}'.");

    private static JsonArray RequireArray(JsonNode? node, string name) =>
        node as JsonArray ?? throw new JsonException($"Expected array for '{name}'.");

    private static string RequireString(JsonObject obj, string propertyName) => RequireNode(obj, propertyName).GetValue<string>();

    private static int RequireInt32(JsonObject obj, string propertyName) => RequireNode(obj, propertyName).GetValue<int>();

    private static double RequireDouble(JsonObject obj, string propertyName) => RequireNode(obj, propertyName).GetValue<double>();

    private static bool RequireBoolean(JsonObject obj, string propertyName) => RequireNode(obj, propertyName).GetValue<bool>();
}