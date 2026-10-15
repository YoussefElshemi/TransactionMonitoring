using Core.Enums;
using Core.ValueObjects;

namespace Core.Models.Roles;

public record CreateRoleRequest
{
    public required OrganizationId OrganizationId { get; init; }
    public required RoleName Name { get; init; }
    public required RoleDescription? Description { get; init; }
    public required List<Permission> Permissions { get; init; }

    public required ApplicationUserId ComposedByUserId { get; init; }
}
