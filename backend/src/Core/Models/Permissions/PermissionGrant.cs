using Core.Enums;

namespace Core.Models.Permissions;

public record PermissionGrant
{
    public required Permission Permission { get; init; }
    public required PermissionScope Scope { get; init; }
}
