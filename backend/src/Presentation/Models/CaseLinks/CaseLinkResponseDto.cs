namespace Presentation.Models.CaseLinks;

public class CaseLinkResponseDto
{
    public required Guid CaseLinkId { get; init; }
    public required Guid OrganizationId { get; init; }
    public required Guid CaseId { get; init; }
    public required Guid LinkedCaseId { get; init; }
    public required string? Reason { get; init; }
    public required Guid CreatedByUserId { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }

    public required string Mode { get; init; }
}
