using Core.Helpers;
using Core.Models.Roles;
using Core.ValueObjects;
using Presentation.Models.Roles;

namespace Presentation.Mappers.Roles;

public static class CreateRoleRequestDtoMapper
{
    public static CreateRoleRequest ToModel(this CreateRoleRequestDto createRoleRequestDto, ApplicationUserId composedByUserId)
    {
        return new CreateRoleRequest
        {
            OrganizationId = new OrganizationId(createRoleRequestDto.OrganizationId),
            Name = new RoleName(createRoleRequestDto.Body.Name),
            Description = !string.IsNullOrWhiteSpace(createRoleRequestDto.Body.Description)
                ? new RoleDescription(createRoleRequestDto.Body.Description)
                : null,
            Permissions = createRoleRequestDto.Body.Permissions.Select(PermissionHelper.Parse).Distinct().ToList(),
            ComposedByUserId = composedByUserId
        };
    }
}
