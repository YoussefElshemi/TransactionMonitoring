using Core.Enums;
using Core.ValueObjects;

namespace Core.Models.Decisions;

public record ResolveDecisionByIdRequest
{
    public required DecisionId DecisionId { get; init; }
    public required CaseStatus Verdict { get; init; }

    public required CaseDisposition? Disposition { get; init; }
    public required CaseReasonCodeId? CaseReasonCodeId { get; init; }
    public required Message? Note { get; init; }
    public required ApplicationUserId ResolvedByUserId { get; init; }
}
