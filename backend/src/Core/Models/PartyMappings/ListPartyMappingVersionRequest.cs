using Core.Models.Common;
using Core.ValueObjects;

namespace Core.Models.PartyMappings;

public record ListPartyMappingVersionRequest : PagedRequest
{
    public required OrganizationId OrganizationId { get; init; }
}
