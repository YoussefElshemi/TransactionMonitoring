using System.Net;
using Core.Enums;
using Core.Helpers;

namespace Core.Exceptions;

public class PermissionDeniedException(Permission permission, string? scopeDescription = null)
    : DomainException(
        scopeDescription is null
            ? $"The permission '{PermissionHelper.ToPermissionString(permission)}' is required."
            : $"The permission '{PermissionHelper.ToPermissionString(permission)}' is required, and the one held does not reach {scopeDescription}.")
{
    public Permission Permission { get; } = permission;

    public override HttpStatusCode GetStatusCode()
    {
        return HttpStatusCode.Forbidden;
    }

    public override string GetTitle()
    {
        return "Permission denied";
    }

    public override object GetExtensions()
    {
        return new
        {
            RequiredPermission = PermissionHelper.ToPermissionString(Permission)
        };
    }
}
