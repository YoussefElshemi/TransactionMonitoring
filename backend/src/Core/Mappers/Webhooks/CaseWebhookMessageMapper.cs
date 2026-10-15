using Core.Models.Cases;
using Core.Models.Webhooks;

namespace Core.Mappers.Webhooks;

public static class CaseWebhookMessageMapper
{
    public static CaseWebhookMessage ToWebhook(this Case @case)
    {
        return new CaseWebhookMessage
        {
            CaseId = @case.CaseId,
            FriendlyName = @case.FriendlyName,
            Status = @case.Status.ToString(),
            Severity = @case.Severity.ToString(),
            Disposition = @case.Disposition?.ToString(),
            ReasonCode = @case.ReasonCode?.Code.ToString(),
            OrganizationId = @case.OrganizationId,
            AssignedToUser = @case.AssignedToUser?.ToWebhook(),
            CreatedAt = @case.CreatedAt,
            UpdatedAt = @case.UpdatedAt,
            AssignedAt = @case.AssignedAt,
            DueAt = @case.DueAt,
            EscalatedAt = @case.EscalatedAt,
            EscalationRuleId = @case.EscalationRuleId,
            ResolvedAt = @case.ResolvedAt,
            ReopenedAt = @case.ReopenedAt
        };
    }
}