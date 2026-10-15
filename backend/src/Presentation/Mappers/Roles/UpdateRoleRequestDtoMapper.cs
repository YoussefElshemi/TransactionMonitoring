using Core.Helpers;
using Core.Models.Roles;
using Core.ValueObjects;
using Presentation.Models.Roles;

namespace Presentation.Mappers.Roles;

public static class UpdateRoleRequestDtoMapper
{
    public static UpdateRoleRequest ToModel(this UpdateRoleRequestDto updateRoleRequestDto, ApplicationUserId composedByUserId)
    {
        return new UpdateRoleRequest
        {
            RoleId = new RoleId(updateRoleRequestDto.RoleId),
            Name = !string.IsNullOrWhiteSpace(updateRoleRequestDto.Body.Name)
                ? new RoleName(updateRoleRequestDto.Body.Name)
                : null,
            Description = !string.IsNullOrWhiteSpace(updateRoleRequestDto.Body.Description)
                ? new RoleDescription(updateRoleRequestDto.Body.Description)
                : null,
            Permissions = updateRoleRequestDto.Body.Permissions?.Select(PermissionHelper.Parse).Distinct().ToList(),
            ComposedByUserId = composedByUserId
        };
    }
}
