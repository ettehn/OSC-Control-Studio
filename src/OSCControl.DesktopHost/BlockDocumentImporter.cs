using System.Text;
using OSCControl.Compiler.Compiler;
using OSCControl.Compiler.Syntax;

namespace OSCControl.DesktopHost;

internal static class BlockDocumentImporter
{
    public static BlockImportResult Import(CompilationResult compilation)
    {
        ArgumentNullException.ThrowIfNull(compilation);

        var warnings = new List<string>();
        var document = new BlockDocument();

        foreach (var declaration in compilation.Syntax.Declarations)
        {
            switch (declaration)
            {
                case EndpointDeclarationSyntax endpoint:
                    document.Endpoints.Add(ImportEndpoint(endpoint));
                    break;
                case VrchatEndpointDeclarationSyntax vrchatEndpoint:
                    document.Endpoints.Add(ImportVrchatEndpoint(vrchatEndpoint));
                    break;
                case RuleDeclarationSyntax rule:
                {
                    var importedRule = ImportRule(rule, warnings);
                    if (importedRule is not null)
                    {
                        document.Rules.Add(importedRule);
                    }
                    break;
                }
                case StateDeclarationSyntax:
                    break;
                default:
                    warnings.Add($"Skipped unsupported declaration: {declaration.GetType().Name}");
                    break;
            }
        }

        if (document.Endpoints.Count == 0 && document.Rules.Count == 0)
        {
            warnings.Add("No supported blocks were found in this script.");
        }

        return new BlockImportResult(document, warnings);
    }

    private static BlockEndpoint ImportEndpoint(EndpointDeclarationSyntax endpoint)
    {
        var imported = new BlockEndpoint
        {
            Name = endpoint.Name.Name,
            Transport = ParseTransport(endpoint.EndpointType),
        };

        foreach (var property in endpoint.Config.Properties)
        {
            switch (property.Key)
            {
                case "mode":
                    imported.Mode = ParseMode(FormatInputExpression(property.Value));
                    break;
                case "host":
                    imported.Host = StripQuotes(FormatInputExpression(property.Value));
                    break;
                case "port":
                    imported.Port = ParsePort(FormatInputExpression(property.Value));
                    break;
                case "path":
                    imported.Path = StripQuotes(FormatInputExpression(property.Value));
                    break;
                case "codec":
                    imported.Codec = StripQuotes(FormatInputExpression(property.Value));
                    break;
            }
        }

        return imported;
    }

    private static BlockEndpoint ImportVrchatEndpoint(VrchatEndpointDeclarationSyntax declaration)
    {
        var imported = new BlockEndpoint
        {
            Name = "vrchat",
            Transport = BlockEndpointTransport.Vrchat,
            Mode = BlockEndpointMode.Output,
            Host = "127.0.0.1",
            Port = 9001,
            InputPort = 9000,
            Codec = "osc"
        };

        if (declaration.Config is null)
        {
            return imported;
        }

        foreach (var property in declaration.Config.Properties)
        {
            switch (property.Key)
            {
                case "host":
                    imported.Host = StripQuotes(FormatInputExpression(property.Value));
                    break;
                case "outputPort":
                case "outPort":
                case "port":
                    imported.Port = ParsePort(FormatInputExpression(property.Value));
                    break;
                case "inputPort":
                case "inPort":
                    imported.InputPort = ParsePort(FormatInputExpression(property.Value));
                    break;
                case "codec":
                    imported.Codec = StripQuotes(FormatInputExpression(property.Value));
                    break;
            }
        }

        return imported;
    }

    private static BlockRule? ImportRule(RuleDeclarationSyntax rule, ICollection<string> warnings)
    {
        var imported = new BlockRule();
        switch (rule.Trigger)
        {
            case StartupTriggerSyntax:
                imported.Trigger = BlockTriggerKind.Startup;
                break;
            case ReceiveTriggerSyntax receive:
                imported.Trigger = BlockTriggerKind.Receive;
                imported.EndpointName = receive.EndpointName.Name;
                break;
            case VrchatAvatarChangeTriggerSyntax:
                imported.Trigger = BlockTriggerKind.VrchatAvatarChange;
                break;
            case VrchatAvatarParameterTriggerSyntax parameter:
                imported.Trigger = BlockTriggerKind.VrchatParameter;
                imported.EndpointName = parameter.ParameterName.Name;
                break;
            default:
                warnings.Add($"Skipped unsupported trigger: {rule.Trigger.GetType().Name}");
                return null;
        }

        ExtractCondition(rule.Condition, imported, warnings);

        foreach (var statement in rule.Body.Statements)
        {
            var step = ImportStep(statement, warnings);
            if (step is not null)
            {
                imported.Steps.Add(step);
            }
        }

        return imported;
    }

    private static void ExtractCondition(ExpressionSyntax? condition, BlockRule rule, ICollection<string> warnings)
    {
        if (condition is null)
        {
            return;
        }

        var parts = new List<ExpressionSyntax>();
        FlattenAndConditions(condition, parts);
        var remaining = new List<string>();
        foreach (var part in parts)
        {
            if (rule.Trigger == BlockTriggerKind.Receive && TryExtractAddress(part, out var address) && string.IsNullOrWhiteSpace(rule.Address))
            {
                rule.Address = address;
                continue;
            }

            remaining.Add(FormatExpression(part));
        }

        rule.WhenExpression = string.Join(" and ", remaining.Where(static value => !string.IsNullOrWhiteSpace(value)));
    }

    private static void FlattenAndConditions(ExpressionSyntax expression, ICollection<ExpressionSyntax> parts)
    {
        if (expression is BinaryExpressionSyntax binary && string.Equals(binary.Operator, "and", StringComparison.OrdinalIgnoreCase))
        {
            FlattenAndConditions(binary.Left, parts);
            FlattenAndConditions(binary.Right, parts);
            return;
        }

        parts.Add(expression);
    }

    private static bool TryExtractAddress(ExpressionSyntax expression, out string address)
    {
        address = string.Empty;
        if (expression is not BinaryExpressionSyntax binary || binary.Operator != "==")
        {
            return false;
        }

        if (binary.Left is MemberExpressionSyntax leftMember
            && leftMember.Target is IdentifierSyntax identifier
            && identifier.Name == "msg"
            && leftMember.Member.Name == "address"
            && binary.Right is StringLiteralExpressionSyntax literal)
        {
            address = literal.Value;
            return true;
        }

        return false;
    }

    private static BlockStep? ImportStep(StatementSyntax statement, ICollection<string> warnings)
    {
        return statement switch
        {
            LogStatementSyntax log => new BlockStep
            {
                Kind = BlockStepKind.Log,
                Target = string.IsNullOrWhiteSpace(log.Level) ? "info" : log.Level!,
                Value = FormatInputExpression(log.Value)
            },
            StoreStatementSyntax store => new BlockStep
            {
                Kind = BlockStepKind.Store,
                Target = store.Name.Name,
                Value = FormatInputExpression(store.Value)
            },
            SendStatementSyntax send => ImportSend(send),
            StopStatementSyntax => new BlockStep
            {
                Kind = BlockStepKind.Stop,
            },
            IfStatementSyntax branch => ImportIf(branch, warnings),
            WhileStatementSyntax loop => ImportWhile(loop, warnings),
            BreakStatementSyntax => new BlockStep
            {
                Kind = BlockStepKind.Break,
            },
            ContinueStatementSyntax => new BlockStep
            {
                Kind = BlockStepKind.Continue,
            },
            VrchatAvatarParameterStatementSyntax vrchatParam => new BlockStep
            {
                Kind = BlockStepKind.VrchatParam,
                Target = vrchatParam.ParameterName.Name,
                Value = FormatInputExpression(vrchatParam.Value)
            },
            VrchatInputStatementSyntax vrchatInput => new BlockStep
            {
                Kind = BlockStepKind.VrchatInput,
                Target = vrchatInput.InputName.Name,
                Value = FormatInputExpression(vrchatInput.Value)
            },
            VrchatChatStatementSyntax vrchatChat => new BlockStep
            {
                Kind = BlockStepKind.VrchatChat,
                Value = FormatInputExpression(vrchatChat.Text),
                Extra = FormatVrchatChatOptions(vrchatChat)
            },
            VrchatTypingStatementSyntax vrchatTyping => new BlockStep
            {
                Kind = BlockStepKind.VrchatTyping,
                Value = FormatInputExpression(vrchatTyping.Value)
            },
            _ => ImportUnsupported(statement, warnings)
        };
    }

    private static BlockStep ImportIf(IfStatementSyntax branch, ICollection<string> warnings)
    {
        var step = new BlockStep
        {
            Kind = BlockStepKind.If,
            Value = FormatInputExpression(branch.Condition)
        };

        foreach (var statement in branch.ThenBlock.Statements)
        {
            var child = ImportStep(statement, warnings);
            if (child is not null)
            {
                step.Children.Add(child);
            }
        }

        if (branch.ElseBlock is not null)
        {
            foreach (var statement in branch.ElseBlock.Statements)
            {
                var child = ImportStep(statement, warnings);
                if (child is not null)
                {
                    step.ElseChildren.Add(child);
                }
            }
        }

        return step;
    }
    private static BlockStep ImportWhile(WhileStatementSyntax loop, ICollection<string> warnings)
    {
        var step = new BlockStep
        {
            Kind = BlockStepKind.While,
            Value = FormatInputExpression(loop.Condition)
        };

        foreach (var statement in loop.Body.Statements)
        {
            var child = ImportStep(statement, warnings);
            if (child is not null)
            {
                step.Children.Add(child);
            }
        }

        return step;
    }

    private static string FormatVrchatChatOptions(VrchatChatStatementSyntax chat)
    {
        var parts = new List<string>();
        if (chat.SendValue is not null)
        {
            parts.Add($"send={FormatInputExpression(chat.SendValue)}");
        }

        if (chat.NotifyValue is not null)
        {
            parts.Add($"notify={FormatInputExpression(chat.NotifyValue)}");
        }

        return string.Join(" ", parts);
    }

    private static BlockStep? ImportUnsupported(StatementSyntax statement, ICollection<string> warnings)
    {
        warnings.Add($"Skipped unsupported statement: {statement.GetType().Name}");
        return null;
    }

    private static BlockStep ImportSend(SendStatementSyntax send)
    {
        var step = new BlockStep
        {
            Kind = BlockStepKind.Send,
            Target = send.Target.Name,
        };

        if (send.Payload is null)
        {
            return step;
        }

        foreach (var property in send.Payload.Properties)
        {
            switch (property.Key)
            {
                case "address":
                    step.Value = StripQuotes(FormatInputExpression(property.Value));
                    break;
                case "args":
                    step.PayloadMode = BlockPayloadMode.Args;
                    step.Extra = FormatArgsInput(property.Value);
                    break;
                case "body":
                    step.PayloadMode = BlockPayloadMode.Body;
                    step.Extra = FormatInputExpression(property.Value);
                    break;
            }
        }

        return step;
    }

    private static string FormatArgsInput(ExpressionSyntax expression)
    {
        if (expression is ListLiteralExpressionSyntax list)
        {
            return string.Join(", ", list.Items.Select(FormatInputExpression));
        }

        return FormatInputExpression(expression);
    }

    private static string FormatInputExpression(ExpressionSyntax expression)
    {
        return expression switch
        {
            StringLiteralExpressionSyntax literal => literal.Value,
            _ => FormatExpression(expression)
        };
    }

    private static string FormatExpression(ExpressionSyntax expression)
    {
        return expression switch
        {
            IdentifierSyntax identifier => identifier.Name,
            NumberLiteralExpressionSyntax number => number.Value.ToString(System.Globalization.CultureInfo.InvariantCulture),
            StringLiteralExpressionSyntax text => Quote(text.Value),
            BooleanLiteralExpressionSyntax boolean => boolean.Value ? "true" : "false",
            NullLiteralExpressionSyntax => "null",
            ListLiteralExpressionSyntax list => $"[[{string.Join(", ", list.Items.Select(FormatExpression))}]]",
            ObjectLiteralExpressionSyntax obj => "{ " + string.Join(", ", obj.Properties.Select(property => $"{property.Key}: {FormatExpression(property.Value)}")) + " }",
            CallExpressionSyntax call => $"{FormatExpression(call.Callee)}({string.Join(", ", call.Arguments.Select(FormatExpression))})",
            MemberExpressionSyntax member => $"{FormatExpression(member.Target)}.{member.Member.Name}",
            IndexExpressionSyntax index => $"{FormatExpression(index.Target)}[{FormatExpression(index.Index)}]",
            UnaryExpressionSyntax unary => $"{unary.Operator}{FormatExpression(unary.Operand)}",
            BinaryExpressionSyntax binary => $"{FormatExpression(binary.Left)} {binary.Operator} {FormatExpression(binary.Right)}",
            ParenthesizedExpressionSyntax grouped => $"({FormatExpression(grouped.Expression)})",
            _ => expression.ToString() ?? string.Empty
        };
    }

    private static string StripQuotes(string value)
    {
        if (value.Length >= 2 && value[0] == '"' && value[^1] == '"')
        {
            return value[1..^1];
        }

        return value;
    }

    private static string Quote(string value)
    {
        var builder = new StringBuilder(value.Length + 2);
        builder.Append('"');
        foreach (var character in value)
        {
            if (character is '"' or '\\')
            {
                builder.Append('\\');
            }

            builder.Append(character);
        }

        builder.Append('"');
        return builder.ToString();
    }

    private static BlockEndpointTransport ParseTransport(string endpointType) => endpointType switch
    {
        "osc.udp" => BlockEndpointTransport.OscUdp,
        "ws.client" => BlockEndpointTransport.WsClient,
        "ws.server" => BlockEndpointTransport.WsServer,
        _ => BlockEndpointTransport.OscUdp,
    };

    private static BlockEndpointMode ParseMode(string mode) => mode.Equals("output", StringComparison.OrdinalIgnoreCase)
        ? BlockEndpointMode.Output
        : BlockEndpointMode.Input;

    private static int ParsePort(string value) => int.TryParse(value, out var port) ? port : 9000;
}

internal sealed record BlockImportResult(BlockDocument Document, IReadOnlyList<string> Warnings);

