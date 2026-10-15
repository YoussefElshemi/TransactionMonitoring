using Core.Helpers;
using Core.Models.Roles;
using Presentation.Models.Roles;

namespace Presentation.Mappers.Roles;

public static class RoleMapper
{
    public static RoleResponseDto ToDto(this Role role)
    {
        return new RoleResponseDto
        {
            RoleId = role.RoleId,
            OrganizationId = role.OrganizationId.HasValue ? (Guid)role.OrganizationId.Value : null,
            Name = role.Name,
            Description = role.Description?.ToString(),
            IsBuiltIn = role.IsBuiltIn,

            Permissions = role.Permissions
                .Select(PermissionHelper.ToPermissionString)
                .Order(StringComparer.Ordinal)
                .ToArray(),

            CreatedAt = role.CreatedAt,
            UpdatedAt = role.UpdatedAt
        };
    }

    public static RoleUsageResponseDto ToDto(this RoleUsage roleUsage)
    {
        return new RoleUsageResponseDto
        {
            RoleId = roleUsage.RoleId,
            Name = roleUsage.Name,
            AssignmentCount = roleUsage.AssignmentCount,
            UserIds = roleUsage.UserIds.ConvertAll(x => (Guid)x).ToArray()
        };
    }
}
