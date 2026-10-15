using Core.Models.Permissions;
using Core.ValueObjects;

namespace Core.Models.Roles;

public record AssignRoleRequest
{
    public required OrganizationId OrganizationId { get; init; }
    public required ApplicationUserId UserId { get; init; }
    public required RoleId RoleId { get; init; }
    public required PermissionScope Scope { get; init; }
    public required ApplicationUserId AssignedByUserId { get; init; }
}
