using Core.Models.Common;
using Core.ValueObjects;

namespace Core.Models.Organizations;

public record ListOrganizationUserRequest : PagedRequest
{
    public required OrganizationId OrganizationId { get; init; }
    public required UserName? UserName { get; init; }
}