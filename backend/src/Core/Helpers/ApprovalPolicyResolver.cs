using System.Text.Json.Nodes;
using Core.Enums;
using Core.Mappers;
using Core.Models.Approvals;
using Core.Models.Parties;

namespace Core.Helpers;

public static class ApprovalPolicyResolver
{
    public static bool RequiresDeploymentApproval(ApprovalPolicy? approvalPolicy)
    {
        return approvalPolicy is not null && approvalPolicy.RequireDeploymentApproval;
    }

    public static bool RequiresCaseApproval(
        ApprovalPolicy? approvalPolicy,
        RuleSeverity severity,
        JsonObject payload,
        IReadOnlyList<PartyFacts> parties,
        DateTimeOffset now)
    {
        if (approvalPolicy is null)
        {
            return false;
        }

        return MatchesSeverity(approvalPolicy, severity) || MatchesPayload(approvalPolicy, payload, parties, now);
    }

    public static bool MatchesMinimumSeverity(ApprovalPolicy? approvalPolicy, RuleSeverity severity)
    {
        return approvalPolicy is not null && MatchesSeverity(approvalPolicy, severity);
    }

    public static bool DeclaresPayloadCondition(ApprovalPolicy? approvalPolicy)
    {
        return approvalPolicy?.CaseDecisionMatcher is not null;
    }

    private static bool MatchesSeverity(ApprovalPolicy approvalPolicy, RuleSeverity severity)
    {
        return approvalPolicy.CaseDecisionMinimumSeverity.HasValue &&
               severity >= approvalPolicy.CaseDecisionMinimumSeverity.Value;
    }

    private static bool MatchesPayload(
        ApprovalPolicy approvalPolicy,
        JsonObject payload,
        IReadOnlyList<PartyFacts> parties,
        DateTimeOffset now)
    {
        if (approvalPolicy.CaseDecisionMatcher is null)
        {
            return false;
        }

        return RuleHelper.Matches(
            approvalPolicy.CaseDecisionMatcher.ToModel(),
            payload,
            RuleMatchPolarity.MatchesWhenTrue,
            now,
            parties);
    }
}
