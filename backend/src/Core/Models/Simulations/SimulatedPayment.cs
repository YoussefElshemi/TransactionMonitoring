using System.Text.Json.Nodes;
using Core.ValueObjects;

namespace Core.Models.Simulations;

public record SimulatedPayment
{
    public required JsonObject Payload { get; init; }
    public required ExternalReference ExternalReference { get; init; }
}
