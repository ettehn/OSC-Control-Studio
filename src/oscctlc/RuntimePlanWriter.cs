using OSCControl.Compiler.Runtime;

namespace oscctlc;

internal static class RuntimePlanWriter
{
    public static void Write(RuntimePlan? plan)
    {
        if (plan is null)
        {
            Console.WriteLine("No runtime plan available.");
            return;
        }

        WritePlan(plan, 0);
    }

    private static void WritePlan(RuntimePlan plan, int indent)
    {
        WriteNode(indent, "RuntimePlan");

        WriteNode(indent + 1, $"Endpoints ({plan.Endpoints.Count})");
        foreach (var endpoint in plan.Endpoints)
        {
            WriteNode(indent + 2, $"RuntimeEndpointPlan {endpoint.Name} : {endpoint.TransportKind}");
            foreach (var property in endpoint.Config)
            {
                WriteExpression(property.Key, property.Value, indent + 3);
            }
        }

        WriteNode(indent + 1, $"States ({plan.States.Count})");
        foreach (var state in plan.States)
        {
            WriteNode(indent + 2, $"RuntimeStatePlan {state.Name}");
            WriteExpression("InitialValue", state.InitialValue, indent + 3);
        }

        WriteNode(indent + 1, $"Rules ({plan.Rules.Count})");
        foreach (var rule in plan.Rules)
        {
            WriteNode(indent + 2, $"RuntimeRulePlan #{rule.Order}");
            WriteTrigger(rule.Trigger, indent + 3);
            if (rule.Guard is not null)
            {
                WriteExpression("Guard", rule.Guard, indent + 3);
            }
            WriteNode(indent + 3, $"Steps ({rule.Steps.Count})");
            foreach (var step in rule.Steps)
            {
                WriteStep(step, indent + 4);
            }
        }
    }

    private static void WriteTrigger(RuntimeTriggerPlan trigger, int indent)
    {
        switch (trigger)
        {
            case RuntimeReceiveTriggerPlan receive:
                WriteNode(indent, $"RuntimeReceiveTriggerPlan {receive.EndpointName}");
                break;
            case RuntimeAddressTriggerPlan address:
                WriteNode(indent, $"RuntimeAddressTriggerPlan \"{address.Address}\"");
                break;
            case RuntimeTimerTriggerPlan timer:
                WriteNode(indent, $"RuntimeTimerTriggerPlan {timer.Interval}");
                break;
            case RuntimeStartupTriggerPlan:
                WriteNode(indent, "RuntimeStartupTriggerPlan");
                break;
        }
    }

    private static void WriteStep(RuntimeStepPlan step, int indent)
    {
        switch (step)
        {
            case RuntimeTransportSendPlan send:
                WriteNode(indent, $"RuntimeTransportSendPlan {send.TargetEndpoint}");
                WriteMessage(send.Message, indent + 1);
                break;
            case RuntimeStateStorePlan store:
                WriteNode(indent, $"RuntimeStateStorePlan {store.StateName}");
                WriteExpression("Value", store.Value, indent + 1);
                break;
            case RuntimeAssignPlan assign:
                WriteNode(indent, "RuntimeAssignPlan");
                WriteExpression("Target", assign.Target, indent + 1);
                WriteExpression("Value", assign.Value, indent + 1);
                break;
            case RuntimeLogPlan log:
                WriteNode(indent, $"RuntimeLogPlan {log.Level}");
                WriteExpression("Value", log.Value, indent + 1);
                break;
            case RuntimeBranchPlan branch:
                WriteNode(indent, "RuntimeBranchPlan");
                WriteExpression("Condition", branch.Condition, indent + 1);
                WriteNode(indent + 1, $"ThenSteps ({branch.ThenSteps.Count})");
                foreach (var nested in branch.ThenSteps)
                {
                    WriteStep(nested, indent + 2);
                }
                if (branch.ElseSteps is not null)
                {
                    WriteNode(indent + 1, $"ElseSteps ({branch.ElseSteps.Count})");
                    foreach (var nested in branch.ElseSteps)
                    {
                        WriteStep(nested, indent + 2);
                    }
                }
                break;
            case RuntimeForEachPlan loop:
                WriteNode(indent, $"RuntimeForEachPlan {loop.IteratorName}");
                WriteExpression("Source", loop.Source, indent + 1);
                WriteNode(indent + 1, $"Body ({loop.Body.Count})");
                foreach (var nested in loop.Body)
                {
                    WriteStep(nested, indent + 2);
                }
                break;
            case RuntimeInvokePlan invoke:
                WriteNode(indent, $"RuntimeInvokePlan {invoke.Name}");
                for (var i = 0; i < invoke.Arguments.Count; i++)
                {
                    WriteExpression($"Arg[{i}]", invoke.Arguments[i], indent + 1);
                }
                break;
            case RuntimeStopPlan:
                WriteNode(indent, "RuntimeStopPlan");
                break;
            case RuntimeLetPlan let:
                WriteNode(indent, $"RuntimeLetPlan {let.Name}");
                WriteExpression("Value", let.Value, indent + 1);
                break;
        }
    }

    private static void WriteMessage(RuntimeMessagePlan message, int indent)
    {
        WriteNode(indent, "Message");
        if (message.Address is not null) WriteExpression("Address", message.Address, indent + 1);
        if (message.Args is not null) WriteExpression("Args", message.Args, indent + 1);
        if (message.Body is not null) WriteExpression("Body", message.Body, indent + 1);
        if (message.Headers is not null) WriteExpression("Headers", message.Headers, indent + 1);
        foreach (var extra in message.Extras)
        {
            WriteExpression(extra.Key, extra.Value, indent + 1);
        }
    }

    private static void WriteExpression(string label, RuntimeExpressionPlan expression, int indent)
    {
        switch (expression)
        {
            case RuntimeIdentifierPlan identifier:
                WriteNode(indent, $"{label}: RuntimeIdentifier({identifier.Name})");
                break;
            case RuntimeNumberPlan number:
                WriteNode(indent, $"{label}: RuntimeNumber({number.Value})");
                break;
            case RuntimeStringPlan str:
                WriteNode(indent, $"{label}: RuntimeString(\"{str.Value}\")");
                break;
            case RuntimeBooleanPlan boolean:
                WriteNode(indent, $"{label}: RuntimeBoolean({boolean.Value})");
                break;
            case RuntimeNullPlan:
                WriteNode(indent, $"{label}: RuntimeNull");
                break;
            case RuntimeListPlan list:
                WriteNode(indent, $"{label}: RuntimeList");
                for (var i = 0; i < list.Items.Count; i++)
                {
                    WriteExpression($"Item[{i}]", list.Items[i], indent + 1);
                }
                break;
            case RuntimeObjectPlan obj:
                WriteNode(indent, $"{label}: RuntimeObject");
                foreach (var property in obj.Properties)
                {
                    WriteExpression(property.Key, property.Value, indent + 1);
                }
                break;
            case RuntimeCallPlan call:
                WriteNode(indent, $"{label}: RuntimeCall");
                WriteExpression("Callee", call.Callee, indent + 1);
                for (var i = 0; i < call.Arguments.Count; i++)
                {
                    WriteExpression($"Arg[{i}]", call.Arguments[i], indent + 1);
                }
                break;
            case RuntimeMemberPlan member:
                WriteNode(indent, $"{label}: RuntimeMember(.{member.Member})");
                WriteExpression("Target", member.Target, indent + 1);
                break;
            case RuntimeIndexPlan index:
                WriteNode(indent, $"{label}: RuntimeIndex");
                WriteExpression("Target", index.Target, indent + 1);
                WriteExpression("Index", index.Index, indent + 1);
                break;
            case RuntimeUnaryPlan unary:
                WriteNode(indent, $"{label}: RuntimeUnary({unary.Operator})");
                WriteExpression("Operand", unary.Operand, indent + 1);
                break;
            case RuntimeBinaryPlan binary:
                WriteNode(indent, $"{label}: RuntimeBinary({binary.Operator})");
                WriteExpression("Left", binary.Left, indent + 1);
                WriteExpression("Right", binary.Right, indent + 1);
                break;
            case RuntimeParenthesizedPlan paren:
                WriteNode(indent, $"{label}: RuntimeParenthesized");
                WriteExpression("Expression", paren.Expression, indent + 1);
                break;
        }
    }

    private static void WriteNode(int indent, string text)
    {
        Console.WriteLine($"{new string(' ', indent * 2)}- {text}");
    }
}


