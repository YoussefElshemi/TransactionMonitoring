using Core.Enums;
using Core.Models.Common;
using Core.ValueObjects;

namespace Core.Models.Parties;

public record ListPartyCaseRequest : PagedRequest
{
    public required OrganizationId OrganizationId { get; init; }
    public required PartyId PartyId { get; init; }
    public required CaseStatus? Status { get; init; }
}
