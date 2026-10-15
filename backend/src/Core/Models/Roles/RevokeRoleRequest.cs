using Core.ValueObjects;

namespace Core.Models.Roles;

public record RevokeRoleRequest
{
    public required OrganizationId OrganizationId { get; init; }
    public required RoleAssignmentId RoleAssignmentId { get; init; }
}
