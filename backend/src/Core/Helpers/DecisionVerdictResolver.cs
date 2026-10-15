using Core.Enums;
using Core.Models.Decisions;

namespace Core.Helpers;

public static class DecisionVerdictResolver
{
    private static readonly DecisionStatus[] TerminalStatuses =
    [
        DecisionStatus.Released,
        DecisionStatus.Blocked,
        DecisionStatus.Expired
    ];

    public static DecisionVerdict ForAction(RuleAction action, bool anyMatched, bool hasCase)
    {
        return action switch
        {
            RuleAction.Block => Verdict(DecisionStatus.Blocked, DecisionReasonCode.RuleBlocked),
            RuleAction.Review when hasCase => Verdict(DecisionStatus.Held, DecisionReasonCode.RuleReviewRequired),
            RuleAction.Review => Verdict(DecisionStatus.Released, DecisionReasonCode.RuleReviewRequired),
            RuleAction.Flag => Verdict(DecisionStatus.Released, DecisionReasonCode.RuleFlagged),
            _ when anyMatched => Verdict(DecisionStatus.Released, DecisionReasonCode.RuleAllowed),
            _ => Verdict(DecisionStatus.Released, DecisionReasonCode.NoRuleMatched)
        };
    }

    public static DecisionVerdict ForCaseStatus(CaseStatus caseStatus)
    {
        return caseStatus switch
        {
            CaseStatus.Approved => Verdict(DecisionStatus.Released, DecisionReasonCode.AnalystApproved),
            CaseStatus.Rejected => Verdict(DecisionStatus.Blocked, DecisionReasonCode.AnalystRejected),
            _ => Verdict(DecisionStatus.Held, DecisionReasonCode.RuleReviewRequired)
        };
    }

    public static DecisionVerdict ForHoldExpiry(HoldExpiryVerdict holdExpiryVerdict)
    {
        return holdExpiryVerdict switch
        {
            HoldExpiryVerdict.Release => Verdict(DecisionStatus.Released, DecisionReasonCode.HoldExpired),
            HoldExpiryVerdict.Block => Verdict(DecisionStatus.Blocked, DecisionReasonCode.HoldExpired),
            HoldExpiryVerdict.Expire => Verdict(DecisionStatus.Expired, DecisionReasonCode.HoldExpired),
            _ => throw new ArgumentOutOfRangeException(nameof(holdExpiryVerdict), holdExpiryVerdict, "A hold that never expires has no expiry verdict.")
        };
    }

    public static bool IsTerminal(this DecisionStatus status)
    {
        return TerminalStatuses.Contains(status);
    }

    private static DecisionVerdict Verdict(DecisionStatus status, DecisionReasonCode reasonCode)
    {
        return new DecisionVerdict
        {
            Status = status,
            ReasonCode = reasonCode
        };
    }
}
