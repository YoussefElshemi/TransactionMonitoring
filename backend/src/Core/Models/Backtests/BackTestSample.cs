using System.Text.Json.Nodes;
using Core.ValueObjects;

namespace Core.Models.BackTests;

public record BackTestSample
{
    public required RuleEvaluationId RuleEvaluationId { get; init; }
    public required TransactionId TransactionId { get; init; }
    public required JsonObject Payload { get; init; }
    public required Matched Matched { get; init; }
    public required CreatedAt CreatedAt { get; init; }
}
