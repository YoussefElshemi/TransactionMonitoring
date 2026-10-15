namespace Presentation.Models.CaseReasonCodes;

public record CaseReasonCodeUsageResponseDto
{
    public required Guid CaseReasonCodeId { get; init; }
    public required string Code { get; init; }
    public required int CaseCount { get; init; }
}
