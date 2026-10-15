using Core.ValueObjects;

namespace Core.Models.Rules;

public record CopyRuleRequest
{
    public required RuleId RuleId { get; init; }
    public required RuleSetId TargetRuleSetId { get; init; }
    public required ApplicationUserId CreatedByUserId { get; init; }
}