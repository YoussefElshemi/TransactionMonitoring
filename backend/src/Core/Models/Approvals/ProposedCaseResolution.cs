using Core.Enums;
using Core.ValueObjects;

namespace Core.Models.Approvals;

public record ProposedCaseResolution
{
    public required CaseStatus Status { get; init; }
    public required CaseDisposition Disposition { get; init; }
    public required CaseReasonCodeId? CaseReasonCodeId { get; init; }
    public required Message? Note { get; init; }
}
