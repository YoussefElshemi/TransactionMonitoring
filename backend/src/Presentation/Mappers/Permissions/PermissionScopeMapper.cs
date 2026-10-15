using Core.Enums;
using Core.Models.Permissions;
using Core.ValueObjects;
using Presentation.Models.Permissions;
using Presentation.Models.Roles;

namespace Presentation.Mappers.Permissions;

public static class PermissionScopeMapper
{
    public static PermissionScopeDto ToDto(this PermissionScope permissionScope)
    {
        return new PermissionScopeDto
        {
            ScopeType = permissionScope.ScopeType.ToString(),
            SubOrganizationId = permissionScope.SubOrganizationId.HasValue ? (Guid)permissionScope.SubOrganizationId.Value : null,
            RuleSetId = permissionScope.RuleSetId.HasValue ? (Guid)permissionScope.RuleSetId.Value : null,
            QueueId = permissionScope.QueueId.HasValue ? (Guid)permissionScope.QueueId.Value : null
        };
    }

    public static PermissionScope ToModel(this AssignRoleRequestBodyDto body)
    {
        if (string.IsNullOrWhiteSpace(body.ScopeType))
        {
            return PermissionScope.Organization();
        }

        return Enum.Parse<PermissionScopeType>(body.ScopeType, true) switch
        {
            PermissionScopeType.SubOrganization => PermissionScope.ForSubOrganization(new SubOrganizationId(body.SubOrganizationId!.Value)),
            PermissionScopeType.RuleSet => PermissionScope.ForRuleSet(new RuleSetId(body.RuleSetId!.Value)),
            PermissionScopeType.Queue => PermissionScope.ForQueue(new QueueId(body.QueueId!.Value)),
            _ => PermissionScope.Organization()
        };
    }
}
