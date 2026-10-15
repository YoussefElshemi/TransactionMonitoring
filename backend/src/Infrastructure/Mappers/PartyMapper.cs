using Core.Enums;
using Core.Models.Parties;
using Core.ValueObjects;
using Infrastructure.Entities;

namespace Infrastructure.Mappers;

public static class PartyMapper
{
    public static Party ToModel(this PartyEntity partyEntity, List<PartyRole> roles)
    {
        return new Party
        {
            Mode = (Mode)partyEntity.ModeId,
            PartyId = new PartyId(partyEntity.PartyId),
            OrganizationId = new OrganizationId(partyEntity.OrganizationId),
            Identifier = new PartyIdentifier(partyEntity.Identifier),
            DisplayName = partyEntity.DisplayName is null ? null : new PartyDisplayName(partyEntity.DisplayName),
            RiskScore = partyEntity.RiskScore.HasValue ? new RiskScore(partyEntity.RiskScore.Value) : null,
            RiskNote = partyEntity.RiskNote is null ? null : new RiskNote(partyEntity.RiskNote),
            RiskSetByUserId = partyEntity.RiskSetByUserId.HasValue ? new ApplicationUserId(partyEntity.RiskSetByUserId.Value) : null,
            RiskSetAt = partyEntity.RiskSetAt.HasValue ? new RiskSetAt(partyEntity.RiskSetAt.Value) : null,
            FirstSeenAt = new FirstSeenAt(partyEntity.FirstSeenAt),
            LastSeenAt = new LastSeenAt(partyEntity.LastSeenAt),
            Attributes = partyEntity.Attributes.AttributesToModel(),
            Roles = roles,
            CreatedAt = new CreatedAt(partyEntity.CreatedAt),
            UpdatedAt = new UpdatedAt(partyEntity.UpdatedAt)
        };
    }

    public static PartyEntity ToEntity(this Party party)
    {
        return new PartyEntity
        {
            ModeId = (int)party.Mode,
            PartyId = party.PartyId,
            OrganizationId = party.OrganizationId,
            Identifier = party.Identifier,
            DisplayName = party.DisplayName?.ToString(),
            RiskScore = party.RiskScore,
            RiskNote = party.RiskNote?.ToString(),
            RiskSetByUserId = party.RiskSetByUserId,
            RiskSetAt = party.RiskSetAt,
            FirstSeenAt = party.FirstSeenAt,
            LastSeenAt = party.LastSeenAt,
            Attributes = party.Attributes.ToBase64String(),
            CreatedAt = party.CreatedAt,
            UpdatedAt = party.UpdatedAt
        };
    }
}
