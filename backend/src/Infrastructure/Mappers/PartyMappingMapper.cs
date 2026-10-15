using Core.Enums;
using Core.Models.PartyMappings;
using Core.ValueObjects;
using Infrastructure.Entities;

namespace Infrastructure.Mappers;

public static class PartyMappingMapper
{
    public static PartyMapping ToModel(this PartyMappingEntity partyMappingEntity)
    {
        return new PartyMapping
        {
            Mode = (Mode)partyMappingEntity.ModeId,
            PartyMappingId = new PartyMappingId(partyMappingEntity.PartyMappingId),
            OrganizationId = new OrganizationId(partyMappingEntity.OrganizationId),
            Mappings = partyMappingEntity.Mappings.MappingsToModel(),
            CurrentVersionId = new PartyMappingVersionId(partyMappingEntity.CurrentVersionId),
            CurrentVersionNumber = new VersionNumber(partyMappingEntity.CurrentVersionNumber),
            CreatedByUserId = new ApplicationUserId(partyMappingEntity.CreatedByUserId),
            CreatedAt = new CreatedAt(partyMappingEntity.CreatedAt),
            UpdatedAt = new UpdatedAt(partyMappingEntity.UpdatedAt),
            IsDeleted = new IsDeleted(partyMappingEntity.IsDeleted),
            DeletedAt = partyMappingEntity.DeletedAt.HasValue ? new DeletedAt(partyMappingEntity.DeletedAt.Value) : null
        };
    }

    public static PartyMappingEntity ToEntity(this PartyMapping partyMapping)
    {
        return new PartyMappingEntity
        {
            ModeId = (int)partyMapping.Mode,
            PartyMappingId = partyMapping.PartyMappingId,
            OrganizationId = partyMapping.OrganizationId,
            Mappings = partyMapping.Mappings.ToBase64String(),
            CurrentVersionId = partyMapping.CurrentVersionId,
            CurrentVersionNumber = partyMapping.CurrentVersionNumber,
            CreatedByUserId = partyMapping.CreatedByUserId,
            CreatedAt = partyMapping.CreatedAt,
            UpdatedAt = partyMapping.UpdatedAt,
            IsDeleted = partyMapping.IsDeleted,
            DeletedAt = partyMapping.DeletedAt
        };
    }
}
