using Core.Enums;
using Core.Models.PartyMappings;
using Core.ValueObjects;
using Infrastructure.Entities;

namespace Infrastructure.Mappers;

public static class PartyMappingVersionMapper
{
    public static PartyMappingVersion ToModel(this PartyMappingVersionEntity partyMappingVersionEntity)
    {
        return new PartyMappingVersion
        {
            Mode = (Mode)partyMappingVersionEntity.ModeId,
            PartyMappingVersionId = new PartyMappingVersionId(partyMappingVersionEntity.PartyMappingVersionId),
            PartyMappingId = new PartyMappingId(partyMappingVersionEntity.PartyMappingId),
            OrganizationId = new OrganizationId(partyMappingVersionEntity.OrganizationId),
            VersionNumber = new VersionNumber(partyMappingVersionEntity.VersionNumber),
            Mappings = partyMappingVersionEntity.Mappings.MappingsToModel(),
            CreatedByUserId = new ApplicationUserId(partyMappingVersionEntity.CreatedByUserId),
            CreatedAt = new CreatedAt(partyMappingVersionEntity.CreatedAt)
        };
    }

    public static PartyMappingVersionEntity ToEntity(this PartyMappingVersion partyMappingVersion)
    {
        return new PartyMappingVersionEntity
        {
            ModeId = (int)partyMappingVersion.Mode,
            PartyMappingVersionId = partyMappingVersion.PartyMappingVersionId,
            PartyMappingId = partyMappingVersion.PartyMappingId,
            OrganizationId = partyMappingVersion.OrganizationId,
            VersionNumber = partyMappingVersion.VersionNumber,
            Mappings = partyMappingVersion.Mappings.ToBase64String(),
            CreatedByUserId = partyMappingVersion.CreatedByUserId,
            CreatedAt = partyMappingVersion.CreatedAt
        };
    }
}
