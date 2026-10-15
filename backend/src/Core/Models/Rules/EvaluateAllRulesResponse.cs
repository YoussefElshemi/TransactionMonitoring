using Core.Models.Cases;
using Core.Models.Decisions;
using Core.ValueObjects;

namespace Core.Models.Rules;

public record EvaluateAllRulesResponse
{
    public required List<EvaluateRuleResponse> Results { get; init; }
    public required RuleOutcome Outcome { get; init; }
    public required TransactionId TransactionId { get; init; }
    public required ExternalReference? ExternalReference { get; init; }
    public required CaseId? CaseId { get; init; }
    public required Case? Case { get; init; }
    public required Decision? Decision { get; init; }
}