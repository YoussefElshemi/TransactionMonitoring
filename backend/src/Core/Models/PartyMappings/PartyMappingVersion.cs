using Core.Enums;
using Core.ValueObjects;

namespace Core.Models.PartyMappings;

public record PartyMappingVersion
{
    public required PartyMappingVersionId PartyMappingVersionId { get; init; }
    public required PartyMappingId PartyMappingId { get; init; }
    public required OrganizationId OrganizationId { get; init; }
    public required VersionNumber VersionNumber { get; init; }
    public required List<PartyRoleMapping> Mappings { get; init; }
    public required ApplicationUserId CreatedByUserId { get; init; }
    public required CreatedAt CreatedAt { get; init; }

    public required Mode Mode { get; init; }
}
