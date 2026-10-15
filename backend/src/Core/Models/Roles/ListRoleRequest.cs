using Core.Models.Common;
using Core.ValueObjects;

namespace Core.Models.Roles;

public record ListRoleRequest : PagedRequest
{
    public required OrganizationId OrganizationId { get; init; }
    public required RoleName? Name { get; init; }

    public required IsBuiltIn? IsBuiltIn { get; init; }
}
