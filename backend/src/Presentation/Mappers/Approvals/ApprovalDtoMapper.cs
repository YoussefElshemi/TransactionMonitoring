using Core.Models.Approvals;
using Presentation.Mappers.Rules;
using Presentation.Models.Approvals;

namespace Presentation.Mappers.Approvals;

public static class ApprovalDtoMapper
{
    public static ApprovalResponseDto ToDto(this Approval approval)
    {
        return new ApprovalResponseDto
        {
            Mode = approval.Mode.ToString(),
            ApprovalId = approval.ApprovalId,
            OrganizationId = approval.OrganizationId,
            Subject = approval.Subject.ToString(),
            RuleDeploymentId = approval.RuleDeploymentId.HasValue ? approval.RuleDeploymentId.Value : null,
            CaseId = approval.CaseId.HasValue ? approval.CaseId.Value : null,
            SuspiciousActivityReportId = approval.SuspiciousActivityReportId.HasValue
                ? approval.SuspiciousActivityReportId.Value
                : null,
            ProposedCaseResolution = approval.ProposedCaseResolution?.ToDto(),
            Status = approval.Status.ToString(),
            Reason = approval.Reason?.ToString(),
            RequestedByUserId = approval.RequestedByUserId,
            ReviewedByUserId = approval.ReviewedByUserId.HasValue ? approval.ReviewedByUserId.Value : null,
            ReviewedAt = approval.ReviewedAt.HasValue ? (DateTimeOffset)approval.ReviewedAt.Value : null,
            ReviewNote = approval.ReviewNote?.ToString(),
            CreatedAt = approval.CreatedAt,
            UpdatedAt = approval.UpdatedAt
        };
    }

    public static ProposedCaseResolutionResponseDto ToDto(this ProposedCaseResolution proposedCaseResolution)
    {
        return new ProposedCaseResolutionResponseDto
        {
            Status = proposedCaseResolution.Status.ToString(),
            Disposition = proposedCaseResolution.Disposition.ToString(),
            CaseReasonCodeId = proposedCaseResolution.CaseReasonCodeId.HasValue
                ? proposedCaseResolution.CaseReasonCodeId.Value
                : null,
            Note = proposedCaseResolution.Note?.ToString()
        };
    }

    public static ApprovalPolicyResponseDto ToDto(this ApprovalPolicy approvalPolicy)
    {
        return new ApprovalPolicyResponseDto
        {
            Mode = approvalPolicy.Mode.ToString(),
            OrganizationId = approvalPolicy.OrganizationId,
            RequireDeploymentApproval = approvalPolicy.RequireDeploymentApproval,
            CaseDecisionMinimumSeverity = approvalPolicy.CaseDecisionMinimumSeverity?.ToString(),
            CaseDecisionMatcher = approvalPolicy.CaseDecisionMatcher?.ToDto(),
            CreatedAt = approvalPolicy.CreatedAt,
            UpdatedAt = approvalPolicy.UpdatedAt
        };
    }
}
