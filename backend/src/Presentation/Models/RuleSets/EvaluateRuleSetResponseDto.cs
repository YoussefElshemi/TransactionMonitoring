using Presentation.Models.Cases;
using Presentation.Models.Decisions;
using Presentation.Models.Rules;

namespace Presentation.Models.RuleSets;

public record EvaluateRuleSetResponseDto
{
    public required Guid RuleSetId { get; init; }
    public required string FriendlyName { get; init; }
    public required EvaluateRuleResponseDto[] Results { get; init; }
    public required RuleOutcomeResponseDto Outcome { get; init; }
    public required Guid TransactionId { get; init; }
    public required string? TransactionReference { get; init; }
    public required CaseResponseDto? Case { get; init; }
    public required DecisionResponseDto? Decision { get; init; }
}
