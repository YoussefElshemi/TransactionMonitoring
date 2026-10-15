using Core.Enums;
using Core.Helpers;
using Core.Models.Permissions;
using Presentation.Models.Permissions;

namespace Presentation.Mappers.Permissions;

public static class PermissionMapper
{
    private const char Separator = ':';

    public static PermissionResponseDto ToDto(this Permission permission, EffectivePermissions effectivePermissions)
    {
        var name = PermissionHelper.ToPermissionString(permission);
        var separator = name.IndexOf(Separator);

        return new PermissionResponseDto
        {
            Name = name,
            Resource = name[..separator],
            Action = name[(separator + 1)..],
            IsGrantable = PermissionBundle.IsGrantableToCustomRole(permission),
            IsHeld = effectivePermissions.Holds(permission)
        };
    }

    public static EffectivePermissionResponseDto ToDto(this PermissionGrant permissionGrant)
    {
        return new EffectivePermissionResponseDto
        {
            Name = PermissionHelper.ToPermissionString(permissionGrant.Permission),
            Scope = permissionGrant.Scope.ToDto()
        };
    }
}
