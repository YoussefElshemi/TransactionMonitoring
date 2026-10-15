using System.Text.Json.Nodes;
using Core.Models.Parties;
using Core.ValueObjects;

namespace Core.Models.VelocityObservations;

public record RecordVelocityObservationsRequest
{
    public required OrganizationId OrganizationId { get; init; }
    public required TransactionId TransactionId { get; init; }
    public required ObservedAt ObservedAt { get; init; }
    public required JsonObject Payload { get; init; }
    public required IReadOnlyList<PartyFacts> Parties { get; init; }
}
