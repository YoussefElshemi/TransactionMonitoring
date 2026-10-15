using Core.Enums;
using Core.Models.Parties;

namespace Core.Mappers;

public static class PartyFactsMapper
{
    public static PartyFacts ToFacts(this Party party, PartyRole role)
    {
        return new PartyFacts
        {
            PartyId = party.PartyId,
            Role = role,
            Identifier = party.Identifier,
            RiskScore = party.RiskScore,
            FirstSeenAt = party.FirstSeenAt,
            LastSeenAt = party.LastSeenAt
        };
    }
}
