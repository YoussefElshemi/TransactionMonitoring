using Core.Models.Rules;
using Core.ValueObjects;

namespace Core.Models.Decisions;

public record RecordDecisionRequest
{
    public required OrganizationId OrganizationId { get; init; }
    public required TransactionId TransactionId { get; init; }
    public required RuleOutcome Outcome { get; init; }
    public required Matched AnyMatched { get; init; }
    public required HoldPolicy HoldPolicy { get; init; }
    public required CaseId? CaseId { get; init; }
    public required List<RuleEvaluationId> RuleEvaluationIds { get; init; }
}
