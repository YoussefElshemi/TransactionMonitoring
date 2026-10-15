using Core.ValueObjects;

namespace Core.Models.PartyMappings;

public record SetPartyMappingRequest
{
    public required OrganizationId OrganizationId { get; init; }
    public required List<PartyRoleMapping> Mappings { get; init; }
    public required ApplicationUserId CreatedByUserId { get; init; }
}
