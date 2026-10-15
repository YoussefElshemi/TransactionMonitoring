using Core.Enums;
using Core.ValueObjects;

namespace Core.Models.Permissions;

public record EffectivePermissions
{
    public required ApplicationUserId UserId { get; init; }
    public required OrganizationId OrganizationId { get; init; }

    public required SubOrganizationId? SubOrganizationId { get; init; }

    public required List<PermissionGrant> Grants { get; init; }

    public bool Holds(Permission permission)
    {
        return Grants.Exists(x => x.Permission == permission);
    }

    public bool Holds(Permission permission, ResourceScope resourceScope)
    {
        return OrganizationId == resourceScope.OrganizationId &&
               Grants.Exists(x => x.Permission == permission && x.Scope.Covers(resourceScope));
    }

    // Empty means no access, not no filter; an organisation-wide scope means unfiltered.
    public List<PermissionScope> ScopesFor(Permission permission)
    {
        return Grants.FindAll(x => x.Permission == permission).ConvertAll(x => x.Scope);
    }

    public bool IsUnscopedFor(Permission permission)
    {
        return Grants.Exists(x => x.Permission == permission && x.Scope.ScopeType == PermissionScopeType.Organization);
    }
}
