using Core.Enums;
using Core.ValueObjects;

namespace Core.Models.Approvals;

public record CreateApprovalRequest
{
    public required OrganizationId OrganizationId { get; init; }
    public required ApprovalSubject Subject { get; init; }
    public required RuleDeploymentId? RuleDeploymentId { get; init; }
    public required CaseId? CaseId { get; init; }
    public required SuspiciousActivityReportId? SuspiciousActivityReportId { get; init; }
    public required ProposedCaseResolution? ProposedCaseResolution { get; init; }
    public required Message? Reason { get; init; }
    public required ApplicationUserId RequestedByUserId { get; init; }
}
