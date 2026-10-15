using System.Text.Json.Nodes;
using Core.Models.Parties;
using Core.ValueObjects;

namespace Core.Models.VelocityObservations;

public record VelocityPeekSample
{
    public required JsonObject Payload { get; init; }
    public required IReadOnlyList<PartyFacts> Parties { get; init; }
    public required EvaluatedAt EvaluatedAt { get; init; }

    public required TransactionId? TransactionId { get; init; }
}
