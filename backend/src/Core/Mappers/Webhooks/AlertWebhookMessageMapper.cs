using Core.Models.Alerts;
using Core.Models.Webhooks;

namespace Core.Mappers.Webhooks;

public static class AlertWebhookMessageMapper
{
    public static AlertWebhookMessage ToWebhook(this Alert alert)
    {
        return new AlertWebhookMessage
        {
            AlertId = alert.AlertId,
            FriendlyName = alert.FriendlyName,
            Status = alert.Status.ToString(),
            Severity = alert.Severity.ToString(),
            OrganizationId = alert.OrganizationId,
            RuleId = alert.RuleId,
            TransactionId = alert.TransactionId,
            PartyId = alert.PartyId,
            PartyRole = alert.PartyRole?.ToString(),
            CaseId = alert.CaseId,
            SuppressionReason = alert.SuppressionReason?.ToString(),
            SuppressedByAlertId = alert.SuppressedByAlertId,
            RaisedAt = alert.RaisedAt,
            ClosedAt = alert.ClosedAt
        };
    }
}
