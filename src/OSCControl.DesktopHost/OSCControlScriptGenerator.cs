using System.Text;
using System.Text.RegularExpressions;

namespace OSCControl.DesktopHost;

internal static class OSCControlScriptGenerator
{
    public static string Generate(BlockDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var builder = new StringBuilder();
        var endpointNames = BuildEndpointNames(document.Endpoints);

        AppendHeader(builder);
        AppendEndpoints(builder, document, endpointNames);
        AppendStates(builder, document.Rules);
        AppendRules(builder, document, endpointNames);

        return builder.ToString().TrimEnd() + Environment.NewLine;
    }

    private static void AppendHeader(StringBuilder builder)
    {
        builder.AppendLine("# Generated from Blocks");
        builder.AppendLine("# Blocks and script can now round-trip partially. Re-import may still simplify unsupported constructs.");
        builder.AppendLine();
    }

    private static Dictionary<BlockEndpoint, string> BuildEndpointNames(IEnumerable<BlockEndpoint> endpoints)
    {
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var map = new Dictionary<BlockEndpoint, string>();
        var index = 1;

        foreach (var endpoint in endpoints)
        {
            string normalized;
            if (endpoint.Transport == BlockEndpointTransport.Vrchat)
            {
                normalized = MakeUniqueIdentifier("vrchat", "vrchat", used);
                used.Add("vrchat_in");
            }
            else
            {
                var fallback = $"endpoint_{index}";
                normalized = MakeUniqueIdentifier(endpoint.Name, fallback, used);
            }

            map[endpoint] = normalized;
            index++;
        }

        return map;
    }

    private static void AppendEndpoints(StringBuilder builder, BlockDocument document, IReadOnlyDictionary<BlockEndpoint, string> endpointNames)
    {
        var wroteAny = false;
        var vrchatEndpoint = document.Endpoints.FirstOrDefault(endpoint => endpoint.Transport == BlockEndpointTransport.Vrchat)
            ?? (UsesVrchat(document) ? CreateImplicitVrchatEndpoint() : null);

        if (vrchatEndpoint is not null)
        {
            AppendVrchatEndpoint(builder, vrchatEndpoint);
            builder.AppendLine();
            wroteAny = true;
        }

        foreach (var endpoint in document.Endpoints.Where(endpoint => endpoint.Transport != BlockEndpointTransport.Vrchat))
        {
            builder.Append("endpoint ");
            builder.Append(endpointNames[endpoint]);
            builder.Append(": ");
            builder.AppendLine(ToTransportKeyword(endpoint.Transport));
            builder.AppendLine("{");
            builder.Append("    mode: ");
            builder.AppendLine(endpoint.Mode == BlockEndpointMode.Input ? "input" : "output");
            builder.Append("    host: ");
            builder.AppendLine(Quote(endpoint.Host, "127.0.0.1"));
            builder.Append("    port: ");
            builder.AppendLine(Math.Max(endpoint.Port, 1).ToString());
            if (!string.IsNullOrWhiteSpace(endpoint.Path))
            {
                builder.Append("    path: ");
                builder.AppendLine(Quote(endpoint.Path.Trim(), "/"));
            }

            builder.Append("    codec: ");
            builder.AppendLine(QuoteOrBareWord(GetCodec(endpoint), GetDefaultCodec(endpoint.Transport)));
            builder.AppendLine("}");
            builder.AppendLine();
            wroteAny = true;
        }

        if (!wroteAny)
        {
            builder.AppendLine("# Add endpoints in the Blocks tab when you want OSC or WebSocket I/O.");
            builder.AppendLine();
        }
    }

    private static void AppendVrchatEndpoint(StringBuilder builder, BlockEndpoint endpoint)
    {
        builder.AppendLine("vrchat.endpoint {");
        builder.Append("    host: ");
        builder.AppendLine(Quote(endpoint.Host, "127.0.0.1"));
        builder.Append("    inputPort: ");
        builder.AppendLine(Math.Max(endpoint.InputPort, 1).ToString());
        builder.Append("    outputPort: ");
        builder.AppendLine(Math.Max(endpoint.Port, 1).ToString());
        builder.Append("    codec: ");
        builder.AppendLine(QuoteOrBareWord(GetCodec(endpoint), "osc"));
        builder.AppendLine("}");
    }

    private static void AppendStates(StringBuilder builder, IEnumerable<BlockRule> rules)
    {
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var states = new List<string>();

        foreach (var rule in rules)
        {
            CollectStates(rule.Steps, states, used);
        }

        foreach (var state in states)
        {
            builder.Append("state ");
            builder.Append(state);
            builder.AppendLine(" = null");
        }

        if (states.Count > 0)
        {
            builder.AppendLine();
        }
    }

    private static void CollectStates(IEnumerable<BlockStep> steps, ICollection<string> states, ISet<string> used)
    {
        foreach (var step in steps)
        {
            if (step.Kind == BlockStepKind.Store && !string.IsNullOrWhiteSpace(step.Target))
            {
                var stateName = MakeUniqueIdentifier(step.Target, "state_value", used, allowExistingBaseName: true);
                if (!states.Contains(stateName, StringComparer.OrdinalIgnoreCase))
                {
                    states.Add(stateName);
                }
            }

            if (step.Children.Count > 0)
            {
                CollectStates(step.Children, states, used);
            }
        }
    }

    private static void AppendRules(StringBuilder builder, BlockDocument document, IReadOnlyDictionary<BlockEndpoint, string> endpointNames)
    {
        var endpointLookup = endpointNames.ToDictionary(pair => pair.Key.Name.Trim(), pair => pair.Value, StringComparer.OrdinalIgnoreCase);
        var firstInputEndpoint = document.Endpoints.FirstOrDefault(endpoint => endpoint.Transport != BlockEndpointTransport.Vrchat && endpoint.Mode == BlockEndpointMode.Input)
            ?? document.Endpoints.FirstOrDefault(endpoint => endpoint.Mode == BlockEndpointMode.Input);
        var firstOutputEndpoint = document.Endpoints.FirstOrDefault(endpoint => endpoint.Transport != BlockEndpointTransport.Vrchat && endpoint.Mode == BlockEndpointMode.Output)
            ?? document.Endpoints.FirstOrDefault(endpoint => endpoint.Mode == BlockEndpointMode.Output)
            ?? document.Endpoints.FirstOrDefault(endpoint => endpoint.Transport != BlockEndpointTransport.Vrchat)
            ?? document.Endpoints.FirstOrDefault();

        for (var index = 0; index < document.Rules.Count; index++)
        {
            var rule = document.Rules[index];
            AppendRule(builder, rule, endpointLookup, firstInputEndpoint, firstOutputEndpoint, endpointNames);
            if (index < document.Rules.Count - 1)
            {
                builder.AppendLine();
            }
        }

        if (document.Rules.Count == 0)
        {
            builder.AppendLine("on startup [");
            builder.AppendLine("    log info \"ready\"");
            builder.AppendLine("]");
        }
    }

    private static void AppendRule(
        StringBuilder builder,
        BlockRule rule,
        IReadOnlyDictionary<string, string> endpointLookup,
        BlockEndpoint? firstInputEndpoint,
        BlockEndpoint? firstOutputEndpoint,
        IReadOnlyDictionary<BlockEndpoint, string> endpointNames)
    {
        switch (rule.Trigger)
        {
            case BlockTriggerKind.Startup:
                builder.Append("on startup");
                break;
            case BlockTriggerKind.Receive:
                builder.Append("on receive ");
                builder.Append(ResolveEndpointReference(rule.EndpointName, endpointLookup, firstInputEndpoint, endpointNames, "input_1"));
                break;
            case BlockTriggerKind.VrchatAvatarChange:
                builder.Append("on vrchat.avatar_change");
                break;
            case BlockTriggerKind.VrchatParameter:
                builder.Append("on vrchat.param ");
                builder.Append(MakeIdentifier(rule.EndpointName, "AvatarParameter"));
                break;
            default:
                builder.Append("on startup");
                break;
        }

        var conditions = new List<string>();
        if (rule.Trigger == BlockTriggerKind.Receive && !string.IsNullOrWhiteSpace(rule.Address))
        {
            conditions.Add($"msg.address == {Quote(rule.Address.Trim(), "/example")}");
        }

        if (!string.IsNullOrWhiteSpace(rule.WhenExpression))
        {
            conditions.Add(rule.WhenExpression.Trim());
        }

        if (conditions.Count > 0)
        {
            builder.Append(" when ");
            builder.Append(string.Join(" and ", conditions));
        }

        builder.AppendLine(" [");

        var wroteStep = AppendSteps(builder, rule.Steps, endpointLookup, firstOutputEndpoint, endpointNames, 1);
        if (!wroteStep)
        {
            builder.AppendLine("    log info \"todo\"");
        }

        builder.AppendLine("]");
    }

    private static bool AppendSteps(
        StringBuilder builder,
        IEnumerable<BlockStep> steps,
        IReadOnlyDictionary<string, string> endpointLookup,
        BlockEndpoint? firstOutputEndpoint,
        IReadOnlyDictionary<BlockEndpoint, string> endpointNames,
        int indentLevel)
    {
        var wroteAny = false;
        foreach (var step in steps)
        {
            wroteAny |= AppendStep(builder, step, endpointLookup, firstOutputEndpoint, endpointNames, indentLevel);
        }

        return wroteAny;
    }

    private static bool AppendStep(
        StringBuilder builder,
        BlockStep step,
        IReadOnlyDictionary<string, string> endpointLookup,
        BlockEndpoint? firstOutputEndpoint,
        IReadOnlyDictionary<BlockEndpoint, string> endpointNames,
        int indentLevel)
    {
        var indent = new string(' ', indentLevel * 4);
        var nestedIndent = new string(' ', (indentLevel + 1) * 4);

        switch (step.Kind)
        {
            case BlockStepKind.Log:
                builder.Append(indent);
                builder.Append("log ");
                builder.Append(string.IsNullOrWhiteSpace(step.Target) ? "info" : step.Target.Trim().ToLowerInvariant());
                builder.Append(' ');
                builder.AppendLine(FormatFriendlyValue(step.Value, "ready"));
                return true;

            case BlockStepKind.Store:
                if (string.IsNullOrWhiteSpace(step.Target))
                {
                    return false;
                }

                builder.Append(indent);
                builder.Append("store ");
                builder.Append(MakeIdentifier(step.Target, "state_value"));
                builder.Append(" = ");
                builder.AppendLine(FormatExpression(step.Value, "null"));
                return true;

            case BlockStepKind.Send:
            {
                var endpointName = ResolveEndpointReference(step.Target, endpointLookup, firstOutputEndpoint, endpointNames, "output_1");
                builder.Append(indent);
                builder.Append("send ");
                builder.Append(endpointName);
                builder.AppendLine(" {");

                if (!string.IsNullOrWhiteSpace(step.Value))
                {
                    builder.Append(nestedIndent);
                    builder.Append("address: ");
                    builder.AppendLine(Quote(step.Value.Trim(), "/example"));
                }

                switch (step.PayloadMode)
                {
                    case BlockPayloadMode.Args:
                        if (!string.IsNullOrWhiteSpace(step.Extra))
                        {
                            builder.Append(nestedIndent);
                            builder.Append("args: ");
                            builder.AppendLine(FormatArgs(step.Extra));
                        }
                        break;
                    case BlockPayloadMode.Body:
                        if (!string.IsNullOrWhiteSpace(step.Extra))
                        {
                            builder.Append(nestedIndent);
                            builder.Append("body: ");
                            builder.AppendLine(FormatFriendlyValue(step.Extra, string.Empty));
                        }
                        break;
                }

                builder.Append(indent);
                builder.AppendLine("}");
                return true;
            }

            case BlockStepKind.Stop:
                builder.Append(indent);
                builder.AppendLine("stop");
                return true;

            case BlockStepKind.While:
                builder.Append(indent);
                builder.Append("while ");
                builder.Append(FormatExpression(step.Value, "true"));
                builder.AppendLine(" [");
                var wroteBody = AppendSteps(builder, step.Children, endpointLookup, firstOutputEndpoint, endpointNames, indentLevel + 1);
                if (!wroteBody)
                {
                    builder.Append(nestedIndent);
                    builder.AppendLine("break");
                }
                builder.Append(indent);
                builder.AppendLine("]");
                return true;

            case BlockStepKind.Break:
                builder.Append(indent);
                builder.AppendLine("break");
                return true;

            case BlockStepKind.Continue:
                builder.Append(indent);
                builder.AppendLine("continue");
                return true;

            case BlockStepKind.VrchatParam:
                builder.Append(indent);
                builder.Append("vrchat.param ");
                builder.Append(MakeIdentifier(step.Target, "AvatarParameter"));
                builder.Append(" = ");
                builder.AppendLine(FormatExpression(step.Value, "0"));
                return true;

            case BlockStepKind.VrchatInput:
                builder.Append(indent);
                builder.Append("vrchat.input ");
                builder.Append(MakeIdentifier(step.Target, "InputName"));
                builder.Append(" = ");
                builder.AppendLine(FormatExpression(step.Value, "1"));
                return true;

            case BlockStepKind.VrchatChat:
                builder.Append(indent);
                builder.Append("vrchat.chat ");
                builder.Append(FormatFriendlyValue(step.Value, "Hello from OSCControl"));
                if (!string.IsNullOrWhiteSpace(step.Extra))
                {
                    builder.Append(' ');
                    builder.Append(step.Extra.Trim());
                }
                builder.AppendLine();
                return true;

            case BlockStepKind.VrchatTyping:
                builder.Append(indent);
                builder.Append("vrchat.typing ");
                builder.AppendLine(FormatExpression(step.Value, "true"));
                return true;

            default:
                return false;
        }
    }

    private static string ResolveEndpointReference(
        string rawName,
        IReadOnlyDictionary<string, string> endpointLookup,
        BlockEndpoint? fallbackEndpoint,
        IReadOnlyDictionary<BlockEndpoint, string> endpointNames,
        string fallbackIdentifier)
    {
        if (!string.IsNullOrWhiteSpace(rawName) && endpointLookup.TryGetValue(rawName.Trim(), out var mapped))
        {
            return mapped;
        }

        if (!string.IsNullOrWhiteSpace(rawName) && string.Equals(rawName.Trim(), "vrchat", StringComparison.OrdinalIgnoreCase))
        {
            return "vrchat";
        }

        if (fallbackEndpoint is not null && endpointNames.TryGetValue(fallbackEndpoint, out var fallbackMapped))
        {
            return fallbackMapped;
        }

        return MakeIdentifier(rawName, fallbackIdentifier);
    }

    private static string ToTransportKeyword(BlockEndpointTransport transport) => transport switch
    {
        BlockEndpointTransport.OscUdp => "osc.udp",
        BlockEndpointTransport.WsClient => "ws.client",
        BlockEndpointTransport.WsServer => "ws.server",
        BlockEndpointTransport.Vrchat => "osc.udp",
        _ => "osc.udp"
    };

    private static string GetCodec(BlockEndpoint endpoint)
    {
        if (!string.IsNullOrWhiteSpace(endpoint.Codec))
        {
            return endpoint.Codec.Trim();
        }

        return GetDefaultCodec(endpoint.Transport);
    }

    private static string GetDefaultCodec(BlockEndpointTransport transport) => transport switch
    {
        BlockEndpointTransport.OscUdp => "osc",
        BlockEndpointTransport.WsClient => "json",
        BlockEndpointTransport.WsServer => "json",
        BlockEndpointTransport.Vrchat => "osc",
        _ => "osc"
    };

    private static string FormatFriendlyValue(string raw, string fallback)
    {
        var trimmed = string.IsNullOrWhiteSpace(raw) ? fallback : raw.Trim();
        return LooksLikeExpression(trimmed) ? FormatExpression(trimmed, fallback) : Quote(trimmed, fallback);
    }

    private static string FormatExpression(string raw, string fallback)
    {
        var trimmed = string.IsNullOrWhiteSpace(raw) ? fallback : raw.Trim();
        return LooksLikeExpression(trimmed) ? trimmed : Quote(trimmed, fallback);
    }

    private static string FormatArgs(string raw)
    {
        var trimmed = raw.Trim();
        if (trimmed.StartsWith("[[", StringComparison.Ordinal))
        {
            return trimmed;
        }

        return $"[[{trimmed}]]";
    }

    private static bool LooksLikeExpression(string value)
    {
        if (value.StartsWith('"') || value.StartsWith("[[", StringComparison.Ordinal) || value.StartsWith('{') || value.StartsWith('[') || value.StartsWith('('))
        {
            return true;
        }

        if (double.TryParse(value, out _))
        {
            return true;
        }

        if (value.Equals("true", StringComparison.OrdinalIgnoreCase)
            || value.Equals("false", StringComparison.OrdinalIgnoreCase)
            || value.Equals("null", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return value.IndexOfAny(['(', ')', '.', '=', '!', '<', '>', '+', '-', '*', '/', ':']) >= 0;
    }

    private static string QuoteOrBareWord(string value, string fallback)
    {
        var trimmed = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        return Regex.IsMatch(trimmed, "^[A-Za-z_][A-Za-z0-9_.-]*$") ? trimmed : Quote(trimmed, fallback);
    }

    private static string Quote(string value, string fallback)
    {
        var text = string.IsNullOrWhiteSpace(value) ? fallback : value;
        return '"' + text.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal) + '"';
    }

    private static string MakeIdentifier(string raw, string fallback)
    {
        var normalized = NormalizeIdentifier(string.IsNullOrWhiteSpace(raw) ? fallback : raw.Trim());
        return string.IsNullOrWhiteSpace(normalized) ? fallback : normalized;
    }

    private static string MakeUniqueIdentifier(string raw, string fallback, ISet<string> used, bool allowExistingBaseName = false)
    {
        var baseName = MakeIdentifier(raw, fallback);
        if (allowExistingBaseName && used.Contains(baseName))
        {
            return baseName;
        }

        var candidate = baseName;
        var suffix = 2;
        while (!used.Add(candidate))
        {
            candidate = $"{baseName}_{suffix}";
            suffix++;
        }

        return candidate;
    }

    private static string NormalizeIdentifier(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var character in value)
        {
            if (char.IsLetterOrDigit(character) || character == '_')
            {
                builder.Append(character);
            }
            else if (builder.Length == 0 || builder[^1] != '_')
            {
                builder.Append('_');
            }
        }

        var normalized = builder.ToString().Trim('_');
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return string.Empty;
        }

        if (char.IsDigit(normalized[0]))
        {
            normalized = '_' + normalized;
        }

        return normalized;
    }

    private static bool UsesVrchat(BlockDocument document)
    {
        if (document.Endpoints.Any(endpoint => endpoint.Transport == BlockEndpointTransport.Vrchat))
        {
            return true;
        }

        return document.Rules.Any(rule => UsesVrchat(rule.Steps));
    }

    private static bool UsesVrchat(IEnumerable<BlockStep> steps)
    {
        foreach (var step in steps)
        {
            if (step.Kind is BlockStepKind.VrchatParam or BlockStepKind.VrchatInput or BlockStepKind.VrchatChat or BlockStepKind.VrchatTyping)
            {
                return true;
            }

            if (step.Children.Count > 0 && UsesVrchat(step.Children))
            {
                return true;
            }
        }

        return false;
    }

    private static BlockEndpoint CreateImplicitVrchatEndpoint() => new()
    {
        Name = "vrchat",
        Transport = BlockEndpointTransport.Vrchat,
        Mode = BlockEndpointMode.Output,
        Host = "127.0.0.1",
        Port = 9001,
        InputPort = 9000,
        Codec = "osc"
    };
}
