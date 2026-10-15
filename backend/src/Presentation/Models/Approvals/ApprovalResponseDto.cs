namespace Presentation.Models.Approvals;

public record ApprovalResponseDto
{
    public required Guid ApprovalId { get; init; }
    public required Guid OrganizationId { get; init; }
    public required string Subject { get; init; }
    public required Guid? RuleDeploymentId { get; init; }
    public required Guid? CaseId { get; init; }
    public required Guid? SuspiciousActivityReportId { get; init; }
    public required ProposedCaseResolutionResponseDto? ProposedCaseResolution { get; init; }
    public required string Status { get; init; }
    public required string? Reason { get; init; }
    public required Guid RequestedByUserId { get; init; }
    public required Guid? ReviewedByUserId { get; init; }
    public required DateTimeOffset? ReviewedAt { get; init; }
    public required string? ReviewNote { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }

    public required string Mode { get; init; }
}

public record ProposedCaseResolutionResponseDto
{
    public required string Status { get; init; }
    public required string Disposition { get; init; }
    public required Guid? CaseReasonCodeId { get; init; }
    public required string? Note { get; init; }
}
