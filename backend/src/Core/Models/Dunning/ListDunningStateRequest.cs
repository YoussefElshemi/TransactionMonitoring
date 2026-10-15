using Core.Models.Common;
using Core.ValueObjects;

namespace Core.Models.Dunning;

public record ListDunningStateRequest : PagedRequest
{
    public required OrganizationId? OrganizationId { get; init; }

    public required IncludeResolved IncludeResolved { get; init; }
}
