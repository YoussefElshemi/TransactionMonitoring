using System.Text.Json.Nodes;
using Core.ValueObjects;

namespace Core.Models.Rules;

public record EvaluateRuleRequest
{
    public required RuleId RuleId { get; init; }
    public required JsonObject Payload { get; init; }
    public required ExternalReference? ExternalReference { get; init; }
    public required CreateCase CreateCase { get; init; }
}