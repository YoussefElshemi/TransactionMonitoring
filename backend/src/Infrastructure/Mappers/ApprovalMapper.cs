using Core.Enums;
using Core.Models.Approvals;
using Core.ValueObjects;
using Infrastructure.Entities;

namespace Infrastructure.Mappers;

public static class ApprovalMapper
{
    public static Approval ToModel(this ApprovalEntity approvalEntity)
    {
        return new Approval
        {
            Mode = (Mode)approvalEntity.ModeId,
            ApprovalId = new ApprovalId(approvalEntity.ApprovalId),
            OrganizationId = new OrganizationId(approvalEntity.OrganizationId),
            Subject = (ApprovalSubject)approvalEntity.SubjectId,
            RuleDeploymentId = approvalEntity.RuleDeploymentId.HasValue
                ? new RuleDeploymentId(approvalEntity.RuleDeploymentId.Value)
                : null,
            CaseId = approvalEntity.CaseId.HasValue ? new CaseId(approvalEntity.CaseId.Value) : null,
            SuspiciousActivityReportId = approvalEntity.SuspiciousActivityReportId.HasValue
                ? new SuspiciousActivityReportId(approvalEntity.SuspiciousActivityReportId.Value)
                : null,
            ProposedCaseResolution = ToProposedCaseResolution(approvalEntity),
            Status = (ApprovalStatus)approvalEntity.StatusId,
            Reason = !string.IsNullOrWhiteSpace(approvalEntity.Reason) ? new Message(approvalEntity.Reason) : null,
            RequestedByUserId = new ApplicationUserId(approvalEntity.RequestedByUserId),
            RequestedByUser = approvalEntity.RequestedByUser?.ToModel(),
            ReviewedByUserId = approvalEntity.ReviewedByUserId.HasValue
                ? new ApplicationUserId(approvalEntity.ReviewedByUserId.Value)
                : null,
            ReviewedByUser = approvalEntity.ReviewedByUser?.ToModel(),
            ReviewedAt = approvalEntity.ReviewedAt.HasValue ? new ReviewedAt(approvalEntity.ReviewedAt.Value) : null,
            ReviewNote = !string.IsNullOrWhiteSpace(approvalEntity.ReviewNote) ? new Message(approvalEntity.ReviewNote) : null,
            CreatedAt = new CreatedAt(approvalEntity.CreatedAt),
            UpdatedAt = new UpdatedAt(approvalEntity.UpdatedAt)
        };
    }

    public static ApprovalEntity ToEntity(this Approval approval)
    {
        return new ApprovalEntity
        {
            ModeId = (int)approval.Mode,
            ApprovalId = approval.ApprovalId,
            OrganizationId = approval.OrganizationId,
            SubjectId = (int)approval.Subject,
            RuleDeploymentId = approval.RuleDeploymentId.HasValue ? approval.RuleDeploymentId.Value : null,
            CaseId = approval.CaseId.HasValue ? approval.CaseId.Value : null,
            SuspiciousActivityReportId = approval.SuspiciousActivityReportId.HasValue
                ? approval.SuspiciousActivityReportId.Value
                : null,
            ProposedStatusId = approval.ProposedCaseResolution is not null ? (int)approval.ProposedCaseResolution.Status : null,
            ProposedDispositionId = approval.ProposedCaseResolution is not null ? (int)approval.ProposedCaseResolution.Disposition : null,
            ProposedCaseReasonCodeId = approval.ProposedCaseResolution?.CaseReasonCodeId.HasValue == true
                ? approval.ProposedCaseResolution.CaseReasonCodeId!.Value
                : null,
            ProposedNote = approval.ProposedCaseResolution?.Note?.ToString(),
            StatusId = (int)approval.Status,
            Reason = approval.Reason?.ToString(),
            RequestedByUserId = approval.RequestedByUserId,
            ReviewedByUserId = approval.ReviewedByUserId.HasValue ? approval.ReviewedByUserId.Value : null,
            ReviewedAt = approval.ReviewedAt.HasValue ? approval.ReviewedAt.Value : null,
            ReviewNote = approval.ReviewNote?.ToString(),
            CreatedAt = approval.CreatedAt,
            UpdatedAt = approval.UpdatedAt
        };
    }

    private static ProposedCaseResolution? ToProposedCaseResolution(ApprovalEntity approvalEntity)
    {
        if (!approvalEntity.ProposedStatusId.HasValue || !approvalEntity.ProposedDispositionId.HasValue)
        {
            return null;
        }

        return new ProposedCaseResolution
        {
            Status = (CaseStatus)approvalEntity.ProposedStatusId.Value,
            Disposition = (CaseDisposition)approvalEntity.ProposedDispositionId.Value,
            CaseReasonCodeId = approvalEntity.ProposedCaseReasonCodeId.HasValue
                ? new CaseReasonCodeId(approvalEntity.ProposedCaseReasonCodeId.Value)
                : null,
            Note = !string.IsNullOrWhiteSpace(approvalEntity.ProposedNote) ? new Message(approvalEntity.ProposedNote) : null
        };
    }
}
