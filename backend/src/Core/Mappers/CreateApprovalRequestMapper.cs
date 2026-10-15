using Core.Enums;
using Core.Models.Approvals;
using Core.ValueObjects;

namespace Core.Mappers;

public static class CreateApprovalRequestMapper
{
    public static Approval ToModel(this CreateApprovalRequest createApprovalRequest, TimeProvider timeProvider, Mode mode)
    {
        var now = timeProvider.GetUtcNow();

        return new Approval
        {
            Mode = mode,
            ApprovalId = new ApprovalId(Guid.NewGuid()),
            OrganizationId = createApprovalRequest.OrganizationId,
            Subject = createApprovalRequest.Subject,
            RuleDeploymentId = createApprovalRequest.RuleDeploymentId,
            CaseId = createApprovalRequest.CaseId,
            SuspiciousActivityReportId = createApprovalRequest.SuspiciousActivityReportId,
            ProposedCaseResolution = createApprovalRequest.ProposedCaseResolution,
            Status = ApprovalStatus.Pending,
            Reason = createApprovalRequest.Reason,
            RequestedByUserId = createApprovalRequest.RequestedByUserId,
            RequestedByUser = null,
            ReviewedByUserId = null,
            ReviewedByUser = null,
            ReviewedAt = null,
            ReviewNote = null,
            CreatedAt = new CreatedAt(now),
            UpdatedAt = new UpdatedAt(now)
        };
    }
}
