namespace Presentation.Models.SuspiciousActivityReports;

public record SuspiciousActivityReportResponseDto
{
    public required Guid SuspiciousActivityReportId { get; init; }
    public required Guid OrganizationId { get; init; }
    public required Guid CaseId { get; init; }
    public required string FriendlyName { get; init; }
    public required string Status { get; init; }
    public required string Narrative { get; init; }
    public required DateTimeOffset DueAt { get; init; }

    public required bool IsOverdue { get; init; }
    public required Guid OpenedByUserId { get; init; }
    public required Guid? FiledByUserId { get; init; }
    public required DateTimeOffset? FiledAt { get; init; }
    public required string? FilingReference { get; init; }
    public required DateTimeOffset? AcknowledgedAt { get; init; }
    public required string? AcknowledgementReference { get; init; }
    public required string? NotFiledReason { get; init; }
    public required Guid? DecidedByUserId { get; init; }
    public required DateTimeOffset? DecidedAt { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }

    public required string Mode { get; init; }
}
