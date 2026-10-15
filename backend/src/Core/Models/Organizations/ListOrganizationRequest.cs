using Core.Models.Common;
using Core.ValueObjects;

namespace Core.Models.Organizations;

public record ListOrganizationRequest : PagedRequest
{
    public required OrganizationId OrganizationId { get; init; }
    public required FriendlyName? FriendlyName { get; init; }
}