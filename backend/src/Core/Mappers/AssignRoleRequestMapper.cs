using Core.Models.Roles;
using Core.ValueObjects;

namespace Core.Mappers;

public static class AssignRoleRequestMapper
{
    public static RoleAssignment ToModel(this AssignRoleRequest assignRoleRequest, TimeProvider timeProvider)
    {
        return new RoleAssignment
        {
            RoleAssignmentId = new RoleAssignmentId(Guid.NewGuid()),
            UserId = assignRoleRequest.UserId,
            RoleId = assignRoleRequest.RoleId,
            Role = null,
            Scope = assignRoleRequest.Scope,
            AssignedByUserId = assignRoleRequest.AssignedByUserId,
            CreatedAt = new CreatedAt(timeProvider.GetUtcNow()),
            UpdatedAt = new UpdatedAt(timeProvider.GetUtcNow())
        };
    }
}
