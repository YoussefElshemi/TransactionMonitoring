using Core.Models.Cases;
using Core.Models.Decisions;
using Core.ValueObjects;

namespace Core.Models.Rules;

public record EvaluateRuleResponse
{
    public required RuleId RuleId { get; init; }
    public required Rule? Rule { get; init; }
    public required FriendlyName FriendlyName { get; init; }
    public required Matched Matched { get; init; }
    public required RulePolicy Policy { get; init; }
    public required RuleOutcome Outcome { get; init; }
    public required TransactionId TransactionId { get; init; }
    public required ExternalReference? ExternalReference { get; init; }
    public required CaseId? CaseId { get; init; }
    public required Case? Case { get; init; }
    public required Decision? Decision { get; init; }
}