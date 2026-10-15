using Core.Models.Decisions;
using Core.Models.RuleEvaluations;
using Core.Models.Webhooks;

namespace Core.Mappers.Webhooks;

public static class DecisionWebhookMessageMapper
{
    public static DecisionWebhookMessage ToWebhook(this Decision decision)
    {
        return new DecisionWebhookMessage
        {
            DecisionId = decision.DecisionId,
            OrganizationId = decision.OrganizationId,
            TransactionId = decision.TransactionId,
            ExternalReference = decision.ExternalReference?.ToString(),
            Verdict = decision.Status.ToString(),
            ReasonCode = decision.ReasonCode.ToString(),
            CaseId = decision.CaseId,
            ResolvedByUser = decision.ResolvedByUser?.ToWebhook(),
            FiredRules = decision.RuleEvaluations.Where(x => x.Matched).Select(ToWebhook).ToArray(),
            HeldAt = decision.HeldAt,
            ExpiresAt = decision.ExpiresAt,
            HoldExpiryVerdict = decision.HoldExpiryVerdict?.ToString(),
            ResolvedAt = decision.ResolvedAt,
            CreatedAt = decision.CreatedAt,
            UpdatedAt = decision.UpdatedAt
        };
    }

    private static DecisionRuleWebhookMessage ToWebhook(RuleEvaluation ruleEvaluation)
    {
        return new DecisionRuleWebhookMessage
        {
            RuleId = ruleEvaluation.RuleId,
            RuleVersionId = ruleEvaluation.RuleVersionId,
            FriendlyName = ruleEvaluation.Rule!.FriendlyName,
            Action = ruleEvaluation.Rule!.Action.ToString(),
            Severity = ruleEvaluation.Rule!.Severity.ToString(),
            RiskWeight = ruleEvaluation.Rule!.RiskWeight
        };
    }
}
