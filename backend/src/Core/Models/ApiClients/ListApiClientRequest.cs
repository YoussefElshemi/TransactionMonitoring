using Core.Models.Common;
using Core.ValueObjects;

namespace Core.Models.ApiClients;

public record ListApiClientRequest : PagedRequest
{
    public required OrganizationId OrganizationId { get; init; }
    public required FriendlyName? FriendlyName { get; init; }
}
