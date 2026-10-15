using Core.Enums;
using Core.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Infrastructure.Entities;

public static class AuditedRelationRegistry
{
    public static bool TryGetOwner(object entity, out AuditedEntityKey owner)
    {
        switch (entity)
        {
            case ApiClientScopeEntity apiClientScope:
                owner = new AuditedEntityKey(AuditableEntityType.ApiClient, apiClientScope.ApiClientId);
                return true;

            case IdentityUserRole<Guid> userRole:
                owner = new AuditedEntityKey(AuditableEntityType.ApplicationUser, userRole.UserId);
                return true;

            case RolePermissionEntity rolePermission:
                owner = new AuditedEntityKey(AuditableEntityType.Role, rolePermission.RoleId);
                return true;

            default:
                owner = default;
                return false;
        }
    }

    public static object? ApiClientScopes(EntityEntry entry)
    {
        var apiClientId = ((ApiClientEntity)entry.Entity).ApiClientId;

        return Surviving<ApiClientScopeEntity>(
            entry,
            x => x.ApiClientId == apiClientId,
            x => ApiScopeHelper.ToScopeString((ApiScope)x.ApiScopeId));
    }

    public static object? ApplicationUserRoles(EntityEntry entry)
    {
        var userId = ((ApplicationUserEntity)entry.Entity).Id;

        return Surviving<IdentityUserRole<Guid>>(
            entry,
            x => x.UserId == userId,
            x => RoleHelper.TryMapToUserRole(x.RoleId, out var userRole) ? userRole.ToString() : x.RoleId.ToString());
    }

    public static object? RolePermissions(EntityEntry entry)
    {
        var roleId = ((ApplicationRoleEntity)entry.Entity).Id;

        return Surviving<RolePermissionEntity>(
            entry,
            x => x.RoleId == roleId,
            x => PermissionHelper.ToPermissionString((Permission)x.PermissionId));
    }

    private static List<string>? Surviving<TRelation>(
        EntityEntry entry,
        Func<TRelation, bool> belongsToOwner,
        Func<TRelation, string> describe)
        where TRelation : class
    {
        var tracked = entry.Context.ChangeTracker
            .Entries<TRelation>()
            .Where(x => belongsToOwner(x.Entity))
            .ToList();

        if (tracked.Count == 0 && entry.State != EntityState.Added)
        {
            return null;
        }

        return tracked
            .Where(x => x.State != EntityState.Deleted)
            .Select(x => describe(x.Entity))
            .Order(StringComparer.Ordinal)
            .ToList();
    }
}

public readonly record struct AuditedEntityKey(AuditableEntityType EntityType, Guid EntityId);
