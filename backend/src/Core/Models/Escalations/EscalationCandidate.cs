using System.Text.Json.Nodes;
using Core.Models.Cases;
using Core.ValueObjects;

namespace Core.Models.Escalations;

public record EscalationCandidate
{
    public required Case Case { get; init; }
    public required JsonObject Payload { get; init; }

    public required ExpiresAt? HoldExpiresAt { get; init; }
}
