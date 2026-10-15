using System.Text.Json.Nodes;

namespace Presentation.Models.RuleEvaluations;

public record ConditionMatchResponseDto
{
    public required string Field { get; init; }
    public required string? Condition { get; init; }
    public string? Quantifier { get; init; }
    public required JsonNode? Expected { get; init; }
    public required JsonNode? Actual { get; init; }
    public required bool Matched { get; init; }
}
