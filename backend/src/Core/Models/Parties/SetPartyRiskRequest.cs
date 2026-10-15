using Core.ValueObjects;

namespace Core.Models.Parties;

public record SetPartyRiskRequest
{
    public required OrganizationId OrganizationId { get; init; }
    public required PartyId PartyId { get; init; }

    public required RiskScore? RiskScore { get; init; }
    public required RiskNote Note { get; init; }
    public required ApplicationUserId SetByUserId { get; init; }
}
