using Core.Models.PartyMappings;
using Presentation.Models.PartyMappings;

namespace Presentation.Mappers.PartyMappings;

public static class PartyMappingDtoMapper
{
    public static PartyMappingResponseDto ToDto(this PartyMapping partyMapping)
    {
        return new PartyMappingResponseDto
        {
            Mode = partyMapping.Mode.ToString(),
            PartyMappingId = partyMapping.PartyMappingId,
            OrganizationId = partyMapping.OrganizationId,
            Mappings = partyMapping.Mappings.ToDto(),
            CurrentVersionNumber = partyMapping.CurrentVersionNumber,
            IsDeleted = partyMapping.IsDeleted,
            DeletedAt = partyMapping.DeletedAt,
            CreatedAt = partyMapping.CreatedAt,
            UpdatedAt = partyMapping.UpdatedAt
        };
    }

    public static PartyMappingUsageResponseDto ToDto(this PartyMappingUsage usage)
    {
        return new PartyMappingUsageResponseDto
        {
            RuleId = usage.RuleId,
            FriendlyName = usage.FriendlyName,
            IsEnabled = usage.IsEnabled,
            Roles = usage.Roles.ConvertAll(x => x.ToString()).ToArray()
        };
    }

    public static PartyMappingVersionResponseDto ToDto(this PartyMappingVersion partyMappingVersion)
    {
        return new PartyMappingVersionResponseDto
        {
            Mode = partyMappingVersion.Mode.ToString(),
            PartyMappingVersionId = partyMappingVersion.PartyMappingVersionId,
            PartyMappingId = partyMappingVersion.PartyMappingId,
            OrganizationId = partyMappingVersion.OrganizationId,
            VersionNumber = partyMappingVersion.VersionNumber,
            Mappings = partyMappingVersion.Mappings.ToDto(),
            CreatedAt = partyMappingVersion.CreatedAt
        };
    }
}
