using Core.ValueObjects;

namespace Core.Models.Decisions;

public record ResolveDecisionRequest
{
    public required CaseId CaseId { get; init; }
    public required DecisionVerdict Verdict { get; init; }
    public required ApplicationUserId? ResolvedByUserId { get; init; }
}
