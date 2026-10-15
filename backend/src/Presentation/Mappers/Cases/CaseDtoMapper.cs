using Core.Models.Cases;
using Presentation.Mappers.Alerts;
using Presentation.Mappers.CaseReasonCodes;
using Presentation.Mappers.RuleEvaluations;
using Presentation.Models.Cases;

namespace Presentation.Mappers.Cases;

public static class CaseDtoMapper
{
    public static CaseResponseDto ToDto(this Case @case)
    {
        return new CaseResponseDto
        {
            Mode = @case.Mode.ToString(),
            CaseId = @case.CaseId,
            FriendlyName = @case.FriendlyName,
            Status = @case.Status.ToString(),
            Severity = @case.Severity.ToString(),
            Disposition = @case.Disposition?.ToString(),
            ReasonCode = @case.ReasonCode?.ToDto(),
            OrganizationId = @case.OrganizationId,
            TransactionId = @case.TransactionId,
            PartyId = @case.PartyId,
            RuleEvaluations = @case.RuleEvaluations.Select(x => x.ToDto()).ToArray(),
            Alerts = @case.Alerts.Select(x => x.ToDto()).ToArray(),
            QueueId = @case.QueueId,
            QueueName = @case.Queue?.Name.ToString(),
            AssignedToUserId = @case.AssignedToUserId,
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
