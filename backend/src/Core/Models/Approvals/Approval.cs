using Core.Enums;
using Core.Models.BaseModels;
using Core.Models.Users;
using Core.ValueObjects;

namespace Core.Models.Approvals;

public record Approval : BaseModel
{
    public required ApprovalId ApprovalId { get; init; }
    public required OrganizationId OrganizationId { get; init; }
    public required ApprovalSubject Subject { get; init; }

    public required RuleDeploymentId? RuleDeploymentId { get; init; }
    public required CaseId? CaseId { get; init; }
    public required SuspiciousActivityReportId? SuspiciousActivityReportId { get; init; }

    public required ProposedCaseResolution? ProposedCaseResolution { get; init; }
    public required ApprovalStatus Status { get; init; }
    public required Message? Reason { get; init; }
    public required ApplicationUserId RequestedByUserId { get; init; }
    public required ApplicationUser? RequestedByUser { get; init; }

    public required ApplicationUserId? ReviewedByUserId { get; init; }
    public required ApplicationUser? ReviewedByUser { get; init; }
    public required ReviewedAt? ReviewedAt { get; init; }
    public required Message? ReviewNote { get; init; }

    public required Mode Mode { get; init; }
}
