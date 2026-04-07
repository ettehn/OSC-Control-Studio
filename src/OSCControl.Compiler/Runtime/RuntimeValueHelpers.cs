using System.Globalization;
using System.Text.Json;

namespace OSCControl.Compiler.Runtime;

internal static class RuntimeValueHelpers
{
    public static object? CloneValue(object? value) => value switch
    {
        null => null,
        Dictionary<string, object?> dictionary => CloneDictionary(dictionary),
        IReadOnlyDictionary<string, object?> dictionary => CloneDictionary(dictionary),
        List<object?> list => CloneList(list),
        IReadOnlyList<object?> list => CloneList(list),
        RuntimeEventMessage message => message.Clone(),
        _ => value
    };

    public static List<object?> CloneList(IReadOnlyList<object?>? items)
    {
        if (items is null)
        {
            return [];
        }

        var clone = new List<object?>(items.Count);
        foreach (var item in items)
        {
            clone.Add(CloneValue(item));
        }

        return clone;
    }

    public static Dictionary<string, object?> CloneDictionary(IReadOnlyDictionary<string, object?>? items)
    {
        var clone = new Dictionary<string, object?>(StringComparer.Ordinal);
        if (items is null)
        {
            return clone;
        }

        foreach (var pair in items)
        {
            clone[pair.Key] = CloneValue(pair.Value);
        }

        return clone;
    }

    public static bool IsTruthy(object? value) => value switch
    {
        null => false,
        bool boolean => boolean,
        string text => !string.IsNullOrEmpty(text),
        sbyte number => number != 0,
        byte number => number != 0,
        short number => number != 0,
        ushort number => number != 0,
        int number => number != 0,
        uint number => number != 0,
        long number => number != 0,
        ulong number => number != 0,
        float number => Math.Abs(number) > double.Epsilon,
        double number => Math.Abs(number) > double.Epsilon,
        decimal number => number != 0,
        IReadOnlyCollection<object?> collection => collection.Count > 0,
        IReadOnlyDictionary<string, object?> dictionary => dictionary.Count > 0,
        _ => true
    };

    public static double ToNumber(object? value) => value switch
    {
        null => 0d,
        double number => number,
        float number => number,
        decimal number => (double)number,
        int number => number,
        long number => number,
        short number => number,
        byte number => number,
        bool boolean => boolean ? 1d : 0d,
        string text when double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) => parsed,
        _ => throw new InvalidOperationException($"Cannot convert value '{value}' to number.")
    };

    public static string ToStringValue(object? value) => value switch
    {
        null => string.Empty,
        string text => text,
        bool boolean => boolean ? "true" : "false",
        _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty
    };

    public static bool ToBoolean(object? value) => value switch
    {
        bool boolean => boolean,
        string text when bool.TryParse(text, out var parsed) => parsed,
        string text when double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var numeric) => Math.Abs(numeric) > double.Epsilon,
        _ => IsTruthy(value)
    };

    public static int ToCount(object? value) => value switch
    {
        null => 0,
        string text => text.Length,
        IReadOnlyCollection<object?> collection => collection.Count,
        IReadOnlyDictionary<string, object?> dictionary => dictionary.Count,
        _ => 1
    };

    public static bool ValuesEqual(object? left, object? right)
    {
        if (left is null && right is null)
        {
            return true;
        }

        if (left is null || right is null)
        {
            return false;
        }

        if (IsNumber(left) && IsNumber(right))
        {
            return Math.Abs(ToNumber(left) - ToNumber(right)) < double.Epsilon;
        }

        return Equals(left, right);
    }

    public static object? GetMember(object? target, string member)
    {
        if (target is null)
        {
            return null;
        }

        if (target is RuntimeEventMessage message)
        {
            return member switch
            {
                "source" => message.SourceEndpoint,
                "address" => message.Address,
                "args" => message.Args,
                "body" => message.Body,
                "headers" => message.Headers,
                "extras" => message.Extras,
                _ => message.Extras.TryGetValue(member, out var extraValue) ? extraValue : null
            };
        }

        if (target is IReadOnlyDictionary<string, object?> dictionary)
        {
            return dictionary.TryGetValue(member, out var memberValue) ? memberValue : null;
        }

        return null;
    }

    public static object? GetIndex(object? target, object? index)
    {
        if (target is null)
        {
            return null;
        }

        if (target is IReadOnlyList<object?> list)
        {
            var numericIndex = Convert.ToInt32(ToNumber(index));
            return numericIndex >= 0 && numericIndex < list.Count ? list[numericIndex] : null;
        }

        if (target is IReadOnlyDictionary<string, object?> dictionary)
        {
            return dictionary.TryGetValue(ToStringValue(index), out var dictionaryValue) ? dictionaryValue : null;
        }

        if (target is string text)
        {
            var numericIndex = Convert.ToInt32(ToNumber(index));
            return numericIndex >= 0 && numericIndex < text.Length ? text[numericIndex].ToString() : null;
        }

        return null;
    }

    public static void SetMember(object? target, string member, object? value)
    {
        switch (target)
        {
            case RuntimeEventMessage message:
                switch (member)
                {
                    case "source":
                        message.SourceEndpoint = ToStringValue(value);
                        return;
                    case "address":
                        message.Address = value is null ? null : ToStringValue(value);
                        return;
                    case "args":
                        message.Args = NormalizeArgs(value);
                        return;
                    case "body":
                        message.Body = CloneValue(value);
                        return;
                    case "headers":
                        message.Headers = NormalizeObject(value);
                        return;
                    case "extras":
                        message.Extras = NormalizeObject(value);
                        return;
                    default:
                        message.Extras[member] = CloneValue(value);
                        return;
                }
            case Dictionary<string, object?> dictionary:
                dictionary[member] = CloneValue(value);
                return;
            default:
                throw new InvalidOperationException($"Cannot assign member '{member}' on target '{target?.GetType().Name ?? "null"}'.");
        }
    }

    public static void SetIndex(object? target, object? index, object? value)
    {
        switch (target)
        {
            case List<object?> list:
                {
                    var numericIndex = Math.Max(0, Convert.ToInt32(ToNumber(index)));
                    while (list.Count <= numericIndex)
                    {
                        list.Add(null);
                    }

                    list[numericIndex] = CloneValue(value);
                    return;
                }
            case Dictionary<string, object?> dictionary:
                dictionary[ToStringValue(index)] = CloneValue(value);
                return;
            default:
                throw new InvalidOperationException($"Cannot assign index on target '{target?.GetType().Name ?? "null"}'.");
        }
    }

    public static object? GetPathValue(object? target, string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return target;
        }

        var current = target;
        foreach (var segment in path.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            current = GetMember(current, segment);
            if (current is null)
            {
                return null;
            }
        }

        return current;
    }

    public static List<object?> NormalizeArgs(object? value) => value switch
    {
        null => [],
        List<object?> list => CloneList(list),
        IReadOnlyList<object?> list => CloneList(list),
        _ => [CloneValue(value)]
    };

    public static Dictionary<string, object?> NormalizeObject(object? value) => value switch
    {
        null => new Dictionary<string, object?>(StringComparer.Ordinal),
        Dictionary<string, object?> dictionary => CloneDictionary(dictionary),
        IReadOnlyDictionary<string, object?> dictionary => CloneDictionary(dictionary),
        RuntimeEventMessage message => new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["source"] = message.SourceEndpoint,
            ["address"] = message.Address,
            ["args"] = CloneList(message.Args),
            ["body"] = CloneValue(message.Body),
            ["headers"] = CloneDictionary(message.Headers),
            ["extras"] = CloneDictionary(message.Extras)
        },
        _ => throw new InvalidOperationException($"Cannot convert value '{value}' to an object.")
    };

    public static string ToJson(object? value) => JsonSerializer.Serialize(value);

    public static object? FromJsonElement(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.Object => element.EnumerateObject().ToDictionary(property => property.Name, property => FromJsonElement(property.Value), StringComparer.Ordinal),
        JsonValueKind.Array => element.EnumerateArray().Select(FromJsonElement).ToList(),
        JsonValueKind.String => element.GetString(),
        JsonValueKind.Number => element.TryGetInt64(out var int64)
            ? int64
            : element.GetDouble(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Null or JsonValueKind.Undefined => null,
        _ => element.GetRawText()
    };

    private static bool IsNumber(object value) => value is sbyte or byte or short or ushort or int or uint or long or ulong or float or double or decimal;
}



