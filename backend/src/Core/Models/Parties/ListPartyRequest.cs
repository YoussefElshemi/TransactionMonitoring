using Core.Enums;
using Core.Models.Common;
using Core.ValueObjects;

namespace Core.Models.Parties;

public record ListPartyRequest : PagedRequest
{
    public required OrganizationId OrganizationId { get; init; }
    public required PartyIdentifier? Identifier { get; init; }
    public required PartyDisplayName? DisplayName { get; init; }
    public required PartyRole? Role { get; init; }

    public required SearchQuery? Query { get; init; }
}
