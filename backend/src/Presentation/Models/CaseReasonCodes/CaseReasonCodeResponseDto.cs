namespace Presentation.Models.CaseReasonCodes;

public record CaseReasonCodeResponseDto
{
    public required Guid CaseReasonCodeId { get; init; }
    public required Guid OrganizationId { get; init; }
    public required string Code { get; init; }
    public required string Description { get; init; }
    public required string? Disposition { get; init; }
    public required bool IsEnabled { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }

    public required string Mode { get; init; }
}
