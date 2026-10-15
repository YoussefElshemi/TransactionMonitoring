using Core.Models.Roles;
using Presentation.Mappers.Permissions;
using Presentation.Models.Roles;

namespace Presentation.Mappers.Roles;

public static class RoleAssignmentMapper
{
    public static RoleAssignmentResponseDto ToDto(this RoleAssignment roleAssignment)
    {
        return new RoleAssignmentResponseDto
        {
            RoleAssignmentId = roleAssignment.RoleAssignmentId,
            UserId = roleAssignment.UserId,
            RoleId = roleAssignment.RoleId,
            RoleName = roleAssignment.Role?.Name.ToString(),
            Scope = roleAssignment.Scope.ToDto(),
            AssignedByUserId = roleAssignment.AssignedByUserId,
            CreatedAt = roleAssignment.CreatedAt
        };
    }
}
