using Core.Models.Roles;
using Core.ValueObjects;
using Presentation.Mappers.Permissions;
using Presentation.Models.Roles;

namespace Presentation.Mappers.Roles;

public static class AssignRoleRequestDtoMapper
{
    public static AssignRoleRequest ToModel(this AssignRoleRequestDto assignRoleRequestDto, ApplicationUserId assignedByUserId)
    {
        return new AssignRoleRequest
        {
            OrganizationId = new OrganizationId(assignRoleRequestDto.OrganizationId),
            UserId = new ApplicationUserId(assignRoleRequestDto.UserId),
            RoleId = new RoleId(assignRoleRequestDto.Body.RoleId),
            Scope = assignRoleRequestDto.Body.ToModel(),
            AssignedByUserId = assignedByUserId
        };
    }

    public static RevokeRoleRequest ToModel(this RevokeRoleRequestDto revokeRoleRequestDto)
    {
        return new RevokeRoleRequest
        {
            OrganizationId = new OrganizationId(revokeRoleRequestDto.OrganizationId),
            RoleAssignmentId = new RoleAssignmentId(revokeRoleRequestDto.RoleAssignmentId)
        };
    }
}
