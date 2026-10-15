using Core.Enums;
using Core.ValueObjects;

namespace Core.Models.Cases;

public record CaseClosure
{
    public required CaseStatus Status { get; init; }
    public required CaseDisposition? Disposition { get; init; }
    public required CaseReasonCodeId? CaseReasonCodeId { get; init; }
}
