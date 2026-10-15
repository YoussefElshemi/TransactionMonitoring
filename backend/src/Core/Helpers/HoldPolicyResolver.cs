using Core.Configs;
using Core.Enums;
using Core.Models.Decisions;
using Core.Models.Organizations;
using Core.Models.Rules;
using Core.ValueObjects;

namespace Core.Helpers;

public static class HoldPolicyResolver
{
    public static HoldPolicy Resolve(IReadOnlyCollection<Rule> firedRules, Organization? organization, DecisionConfig decisionConfig)
    {
        var ruleVerdicts = firedRules.Where(x => x.HoldExpiryVerdict.HasValue).Select(x => x.HoldExpiryVerdict!.Value).ToList();

        var holdExpiryVerdict = ruleVerdicts.Count > 0
            ? ruleVerdicts.Max()
            : organization?.HoldExpiryVerdict ?? decisionConfig.DefaultHoldExpiryVerdict;

        return new HoldPolicy
        {
            HoldWindow = ResolveHoldWindow(firedRules, organization, decisionConfig, holdExpiryVerdict),
            HoldExpiryVerdict = holdExpiryVerdict
        };
    }

    private static HoldWindow? ResolveHoldWindow(
        IReadOnlyCollection<Rule> firedRules,
        Organization? organization,
        DecisionConfig decisionConfig,
        HoldExpiryVerdict holdExpiryVerdict)
    {
        if (holdExpiryVerdict is HoldExpiryVerdict.Never)
        {
            return null;
        }

        var ruleWindows = firedRules.Where(x => x.HoldWindow.HasValue).Select(x => x.HoldWindow!.Value).ToList();

        return ruleWindows.Count > 0
            ? ruleWindows.MaxBy(x => (int)x)
            : organization?.HoldWindow ?? new HoldWindow(decisionConfig.DefaultHoldWindowMinutes);
    }
}
