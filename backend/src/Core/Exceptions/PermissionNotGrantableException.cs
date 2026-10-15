using System.Net;
using Core.Enums;
using Core.Helpers;

namespace Core.Exceptions;

public class PermissionNotGrantableException(IReadOnlyList<Permission> permissions, bool isPlatformPermission)
    : DomainException(
        isPlatformPermission
            ? $"The permission(s) {Summarise(permissions)} cannot be granted through a role."
            : $"The permission(s) {Summarise(permissions)} cannot be granted by somebody who does not hold them.")
{
    public IReadOnlyList<Permission> Permissions { get; } = permissions;

    public override HttpStatusCode GetStatusCode()
    {
        return HttpStatusCode.Forbidden;
    }

    public override string GetTitle()
    {
        return "Permission cannot be granted";
    }

    public override object GetExtensions()
    {
        return new
        {
            Permissions = Permissions.Select(PermissionHelper.ToPermissionString).ToList()
        };
    }

    private static string Summarise(IReadOnlyList<Permission> permissions)
    {
        return string.Join(", ", permissions.Select(x => $"'{PermissionHelper.ToPermissionString(x)}'"));
    }
}
