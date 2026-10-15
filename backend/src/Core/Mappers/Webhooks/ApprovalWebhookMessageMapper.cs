using Core.Models.Approvals;
using Core.Models.Webhooks;

namespace Core.Mappers.Webhooks;

public static class ApprovalWebhookMessageMapper
{
    public static ApprovalWebhookMessage ToWebhook(this Approval approval)
    {
        return new ApprovalWebhookMessage
        {
            ApprovalId = approval.ApprovalId,
            OrganizationId = approval.OrganizationId,
            Subject = approval.Subject.ToString(),
            RuleDeploymentId = approval.RuleDeploymentId.HasValue ? approval.RuleDeploymentId.Value : null,
            CaseId = approval.CaseId.HasValue ? approval.CaseId.Value : null,
            SuspiciousActivityReportId = approval.SuspiciousActivityReportId.HasValue
                ? approval.SuspiciousActivityReportId.Value
                : null,
            Status = approval.Status.ToString(),
            Reason = approval.Reason?.ToString(),
            RequestedByUser = approval.RequestedByUser?.ToWebhook(),
            ReviewedByUser = approval.ReviewedByUser?.ToWebhook(),
            ReviewedAt = approval.ReviewedAt.HasValue ? approval.ReviewedAt.Value : null,
            ReviewNote = approval.ReviewNote?.ToString(),
            CreatedAt = approval.CreatedAt,
            UpdatedAt = approval.UpdatedAt
        };
    }
}
