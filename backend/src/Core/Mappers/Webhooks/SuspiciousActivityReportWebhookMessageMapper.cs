using Core.Models.SuspiciousActivityReports;
using Core.Models.Webhooks;

namespace Core.Mappers.Webhooks;

public static class SuspiciousActivityReportWebhookMessageMapper
{
    public static SuspiciousActivityReportWebhookMessage ToWebhook(this SuspiciousActivityReport suspiciousActivityReport)
    {
        return new SuspiciousActivityReportWebhookMessage
        {
            SuspiciousActivityReportId = suspiciousActivityReport.SuspiciousActivityReportId,
            OrganizationId = suspiciousActivityReport.OrganizationId,
            CaseId = suspiciousActivityReport.CaseId,
            FriendlyName = suspiciousActivityReport.FriendlyName,
            Status = suspiciousActivityReport.Status.ToString(),
            DueAt = suspiciousActivityReport.DueAt,
            OpenedByUser = suspiciousActivityReport.OpenedByUser?.ToWebhook(),
            FiledByUser = suspiciousActivityReport.FiledByUser?.ToWebhook(),
            FiledAt = suspiciousActivityReport.FiledAt.HasValue ? suspiciousActivityReport.FiledAt.Value : null,
            FilingReference = suspiciousActivityReport.FilingReference?.ToString(),
            AcknowledgedAt = suspiciousActivityReport.AcknowledgedAt.HasValue ? suspiciousActivityReport.AcknowledgedAt.Value : null,
            AcknowledgementReference = suspiciousActivityReport.AcknowledgementReference?.ToString(),
            NotFiledReason = suspiciousActivityReport.NotFiledReason?.ToString(),
            CreatedAt = suspiciousActivityReport.CreatedAt,
            UpdatedAt = suspiciousActivityReport.UpdatedAt
        };
    }
}
