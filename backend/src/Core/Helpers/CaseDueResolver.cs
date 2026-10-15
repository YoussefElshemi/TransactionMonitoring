using Core.Configs;
using Core.Enums;
using Core.Models.Decisions;
using Core.ValueObjects;

namespace Core.Helpers;

public static class CaseDueResolver
{
    public static DueAt Resolve(
        RuleSeverity severity,
        HoldPolicy? holdPolicy,
        IReadOnlyDictionary<RuleSeverity, SlaTarget> targets,
        CaseConfig caseConfig,
        DateTimeOffset openedAt)
    {
        var dueAt = openedAt + ResolveTarget(severity, targets, caseConfig).ToTimeSpan();

        if (holdPolicy?.HoldWindow is not null)
        {
            var holdExpiresAt = openedAt + holdPolicy.HoldWindow.Value.ToTimeSpan();

            if (holdExpiresAt < dueAt)
            {
                dueAt = holdExpiresAt;
            }
        }

        return new DueAt(dueAt);
    }

    public static SlaTarget ResolveTarget(
        RuleSeverity severity,
        IReadOnlyDictionary<RuleSeverity, SlaTarget> targets,
        CaseConfig caseConfig)
    {
        if (targets.TryGetValue(severity, out var declared))
        {
            return declared;
        }

        return caseConfig.DefaultSlaTargetMinutes.TryGetValue(severity, out var house)
            ? new SlaTarget(house)
            : new SlaTarget(caseConfig.FallbackSlaTargetMinutes);
    }
}
