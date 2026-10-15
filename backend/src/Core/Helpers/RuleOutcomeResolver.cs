using Core.Enums;
using Core.Models.Rules;
using Core.ValueObjects;

namespace Core.Helpers;

public static class RuleOutcomeResolver
{
    private static readonly RuleAction[] Precedence =
    [
        RuleAction.Allow,
        RuleAction.Flag,
        RuleAction.Review,
        RuleAction.Block
    ];

    private static readonly RuleAction[] OpensCaseActions =
    [
        RuleAction.Review,
        RuleAction.Block
    ];

    public static RuleOutcome Resolve(IReadOnlyCollection<RulePolicy> firedPolicies, ReviewThreshold threshold)
    {
        if (firedPolicies.Count == 0)
        {
            return new RuleOutcome
            {
                Action = RuleAction.Allow,
                Severity = RuleSeverity.Info,
                RiskScore = new RiskScore(0)
            };
        }

        var action = firedPolicies.MaxBy(x => Rank(x.Action))!.Action;
        var severity = firedPolicies.Max(x => x.Severity);
        var riskScore = new RiskScore(firedPolicies.Sum(x => x.RiskWeight));

        if (riskScore > 0 && riskScore >= threshold && Rank(action) < Rank(RuleAction.Review))
        {
            action = RuleAction.Review;
        }

        return new RuleOutcome
        {
            Action = action,
            Severity = severity,
            RiskScore = riskScore
        };
    }

    public static bool OpensCase(this RuleAction action)
    {
        return OpensCaseActions.Contains(action);
    }

    private static int Rank(RuleAction action)
    {
        return Array.IndexOf(Precedence, action);
    }
}
