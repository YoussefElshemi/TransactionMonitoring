using Presentation.Models.Cases;
using Presentation.Models.Decisions;

namespace Presentation.Models.Rules;

public record EvaluateAllRulesResponseDto
{
    public required EvaluateRuleResponseDto[] Results { get; init; }
    public required RuleOutcomeResponseDto Outcome { get; init; }
    public required Guid TransactionId { get; init; }
    public required string? TransactionReference { get; init; }
    public required CaseResponseDto? Case { get; init; }
    public required DecisionResponseDto? Decision { get; init; }
}