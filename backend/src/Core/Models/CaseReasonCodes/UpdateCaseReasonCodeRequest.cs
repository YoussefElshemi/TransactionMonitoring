using Core.Enums;
using Core.ValueObjects;

namespace Core.Models.CaseReasonCodes;

public record UpdateCaseReasonCodeRequest
{
    public required CaseReasonCodeId CaseReasonCodeId { get; init; }
    public required CaseReasonCodeDescription? Description { get; init; }
    public required CaseDisposition? Disposition { get; init; }

    public required ClearDisposition ClearDisposition { get; init; }
    public required IsEnabled? IsEnabled { get; init; }
}
