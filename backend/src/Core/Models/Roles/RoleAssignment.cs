using Core.Models.BaseModels;
using Core.Models.Permissions;
using Core.ValueObjects;

namespace Core.Models.Roles;

public record RoleAssignment : BaseModel
{
    public required RoleAssignmentId RoleAssignmentId { get; init; }
    public required ApplicationUserId UserId { get; init; }
    public required RoleId RoleId { get; init; }

    public required Role? Role { get; init; }

    public required PermissionScope Scope { get; init; }
    public required ApplicationUserId AssignedByUserId { get; init; }
}
