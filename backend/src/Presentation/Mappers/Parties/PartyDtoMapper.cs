using Core.Models.Parties;
using Presentation.Models.Parties;

namespace Presentation.Mappers.Parties;

public static class PartyDtoMapper
{
    public static PartyResponseDto ToDto(this Party party)
    {
        return new PartyResponseDto
        {
            Mode = party.Mode.ToString(),
            PartyId = party.PartyId,
            OrganizationId = party.OrganizationId,
            Identifier = party.Identifier,
            DisplayName = party.DisplayName?.ToString(),
            Roles = party.Roles.ConvertAll(x => x.ToString()).ToArray(),
            RiskScore = party.RiskScore,
            RiskNote = party.RiskNote?.ToString(),
            RiskSetByUserId = party.RiskSetByUserId,
            RiskSetAt = party.RiskSetAt,
            FirstSeenAt = party.FirstSeenAt,
            LastSeenAt = party.LastSeenAt,
            Attributes = party.Attributes.ConvertAll(x => new PartyAttributeResponseDto
            {
                Name = x.Name,
                Value = x.Value
            }).ToArray(),
            CreatedAt = party.CreatedAt,
            UpdatedAt = party.UpdatedAt
        };
    }

    public static TransactionPartyResponseDto ToDto(this TransactionParty transactionParty)
    {
        return new TransactionPartyResponseDto
        {
            TransactionId = transactionParty.TransactionId,
            PartyId = transactionParty.PartyId,
            Role = transactionParty.Role.ToString(),
            Identifier = transactionParty.Party?.Identifier.ToString(),
            DisplayName = transactionParty.Party?.DisplayName?.ToString(),
            CreatedAt = transactionParty.CreatedAt
        };
    }
}
