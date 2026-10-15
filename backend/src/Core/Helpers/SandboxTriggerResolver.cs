using System.Text.Json.Nodes;
using Core.Constants;
using Core.Enums;
using Core.Exceptions;
using Core.Models.Rules;
using Core.ValueObjects;

namespace Core.Helpers;

public static class SandboxTriggerResolver
{
    public static SandboxTrigger? Resolve(Mode mode, JsonObject payload, ExternalReference? externalReference)
    {
        if (mode != Mode.Test)
        {
            return null;
        }

        SandboxTrigger? referenced = externalReference is { } reference &&
                                     SandboxTriggerConstants.Triggers.TryGetValue(reference.ToString(), out var fromReference)
            ? fromReference
            : null;

        return Max(referenced, Descend(payload));
    }

    public static RuleOutcome Apply(SandboxTrigger sandboxTrigger, RuleOutcome outcome)
    {
        var action = sandboxTrigger switch
        {
            SandboxTrigger.Allow => RuleAction.Allow,
            SandboxTrigger.Flag => RuleAction.Flag,
            SandboxTrigger.Review => RuleAction.Review,
            SandboxTrigger.Block => RuleAction.Block,

            SandboxTrigger.Hold => RuleAction.Review,
            _ => outcome.Action
        };

        return outcome with
        {
            Action = action,
            Severity = action == RuleAction.Allow ? outcome.Severity : Max(outcome.Severity, RuleSeverity.Medium)
        };
    }

    public static bool ForcesCase(SandboxTrigger sandboxTrigger)
    {
        return sandboxTrigger is SandboxTrigger.Hold;
    }

    public static void ThrowIfError(Mode mode, JsonObject payload, ExternalReference? externalReference)
    {
        if (Resolve(mode, payload, externalReference) is SandboxTrigger.Error)
        {
            throw new SandboxTriggerException();
        }
    }

    private static RuleSeverity Max(RuleSeverity left, RuleSeverity right)
    {
        return left > right ? left : right;
    }

    private static SandboxTrigger? Max(SandboxTrigger? left, SandboxTrigger? right)
    {
        if (left is null || right is null)
        {
            return left ?? right;
        }

        return left > right ? left : right;
    }

    private static SandboxTrigger? Descend(JsonNode? node)
    {
        switch (node)
        {
            case JsonObject jsonObject:
                return jsonObject.Select(x => Descend(x.Value)).Where(x => x is not null).DefaultIfEmpty(null).Max();

            case JsonArray jsonArray:
                return jsonArray.Select(Descend).Where(x => x is not null).DefaultIfEmpty(null).Max();

            case JsonValue jsonValue when jsonValue.TryGetValue<string>(out var value):
                return SandboxTriggerConstants.Triggers.TryGetValue(value, out var trigger) ? trigger : null;

            default:
                return null;
        }
    }
}
