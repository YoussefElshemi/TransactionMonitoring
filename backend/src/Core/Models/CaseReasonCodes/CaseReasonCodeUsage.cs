using Core.ValueObjects;

namespace Core.Models.CaseReasonCodes;

public record CaseReasonCodeUsage
{
    public required CaseReasonCodeId CaseReasonCodeId { get; init; }
    public required CaseReasonCodeName Code { get; init; }
    public required TotalCount CaseCount { get; init; }
}
