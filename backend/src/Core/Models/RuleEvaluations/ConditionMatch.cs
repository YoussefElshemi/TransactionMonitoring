using System.Text.Json.Nodes;
using Core.Enums;

namespace Core.Models.RuleEvaluations;

public record ConditionMatch
{
    public required string Field { get; init; }
    public required RuleCondition? Condition { get; init; }
    public RuleQuantifier? Quantifier { get; init; }
    public required JsonNode? Expected { get; init; }
    public required JsonNode? Actual { get; init; }
    public required bool Matched { get; init; }
}
