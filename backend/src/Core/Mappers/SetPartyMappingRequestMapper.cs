using Core.Enums;
using Core.Models.PartyMappings;
using Core.ValueObjects;

namespace Core.Mappers;

public static class SetPartyMappingRequestMapper
{
    public static PartyMapping ToModel(this SetPartyMappingRequest setPartyMappingRequest, PartyMappingVersionId currentVersionId, TimeProvider timeProvider, Mode mode)
    {
        return new PartyMapping
        {
            Mode = mode,
            PartyMappingId = new PartyMappingId(Guid.NewGuid()),
            OrganizationId = setPartyMappingRequest.OrganizationId,
            Mappings = setPartyMappingRequest.Mappings,
            CurrentVersionId = currentVersionId,
            CurrentVersionNumber = new VersionNumber(1),
            CreatedByUserId = setPartyMappingRequest.CreatedByUserId,
            CreatedAt = new CreatedAt(timeProvider.GetUtcNow()),
            UpdatedAt = new UpdatedAt(timeProvider.GetUtcNow()),
            IsDeleted = new IsDeleted(false),
            DeletedAt = null
        };
    }

    public static PartyMappingVersion ToVersionModel(
        this SetPartyMappingRequest setPartyMappingRequest,
        PartyMappingVersionId partyMappingVersionId,
        PartyMappingId partyMappingId,
        VersionNumber versionNumber,
        TimeProvider timeProvider, Mode mode)
    {
        return new PartyMappingVersion
        {
            Mode = mode,
            PartyMappingVersionId = partyMappingVersionId,
            PartyMappingId = partyMappingId,
            OrganizationId = setPartyMappingRequest.OrganizationId,
            VersionNumber = versionNumber,
            Mappings = setPartyMappingRequest.Mappings,
            CreatedByUserId = setPartyMappingRequest.CreatedByUserId,
            CreatedAt = new CreatedAt(timeProvider.GetUtcNow())
        };
    }
}
