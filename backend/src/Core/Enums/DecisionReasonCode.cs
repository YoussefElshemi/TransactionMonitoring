namespace Core.Enums;

public enum DecisionReasonCode
{
    NoRuleMatched = 1,
    RuleAllowed,
    RuleFlagged,
    RuleReviewRequired,
    RuleBlocked,
    AnalystApproved,
    AnalystRejected,
    HoldExpired
}
