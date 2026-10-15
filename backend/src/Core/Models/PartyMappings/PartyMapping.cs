using Core.Enums;
using Core.Models.BaseModels;
using Core.ValueObjects;

namespace Core.Models.PartyMappings;

public record PartyMapping : DeletableModel
{
    public required PartyMappingId PartyMappingId { get; init; }
    public required OrganizationId OrganizationId { get; init; }
    public required List<PartyRoleMapping> Mappings { get; init; }
    public required PartyMappingVersionId CurrentVersionId { get; init; }
    public required VersionNumber CurrentVersionNumber { get; init; }
    public required ApplicationUserId CreatedByUserId { get; init; }

    public required Mode Mode { get; init; }
}
