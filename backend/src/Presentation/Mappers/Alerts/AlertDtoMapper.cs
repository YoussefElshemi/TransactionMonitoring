using Core.Models.Alerts;
using Presentation.Models.Alerts;

namespace Presentation.Mappers.Alerts;

public static class AlertDtoMapper
{
    public static AlertResponseDto ToDto(this Alert alert)
    {
        return new AlertResponseDto
        {
            Mode = alert.Mode.ToString(),
            AlertId = alert.AlertId,
            FriendlyName = alert.FriendlyName,
            Status = alert.Status.ToString(),
            Severity = alert.Severity.ToString(),
            OrganizationId = alert.OrganizationId,
            RuleId = alert.RuleId,
            RuleVersionId = alert.RuleVersionId,
            RuleEvaluationId = alert.RuleEvaluationId,
            TransactionId = alert.TransactionId,
            PartyId = alert.PartyId,
            PartyRole = alert.PartyRole?.ToString(),
            CaseId = alert.CaseId,
            SuppressionReason = alert.SuppressionReason?.ToString(),
            SuppressedByAlertId = alert.SuppressedByAlertId,
            RaisedAt = alert.RaisedAt,
            ClosedAt = alert.ClosedAt,
            CreatedAt = alert.CreatedAt,
            UpdatedAt = alert.UpdatedAt
        };
    }
}
