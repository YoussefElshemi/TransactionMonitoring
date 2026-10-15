using Core.Enums;
using Core.ValueObjects;

namespace Core.Models.Permissions;

public record PermissionScope
{
    public required PermissionScopeType ScopeType { get; init; }
    public required SubOrganizationId? SubOrganizationId { get; init; }
    public required RuleSetId? RuleSetId { get; init; }
    public required QueueId? QueueId { get; init; }

    public static PermissionScope Organization()
    {
        return new PermissionScope
        {
            ScopeType = PermissionScopeType.Organization,
            SubOrganizationId = null,
            RuleSetId = null,
            QueueId = null
        };
    }

    public static PermissionScope ForSubOrganization(SubOrganizationId subOrganizationId)
    {
        return new PermissionScope
        {
            ScopeType = PermissionScopeType.SubOrganization,
            SubOrganizationId = subOrganizationId,
            RuleSetId = null,
            QueueId = null
        };
    }

    public static PermissionScope ForRuleSet(RuleSetId ruleSetId)
    {
        return new PermissionScope
        {
            ScopeType = PermissionScopeType.RuleSet,
            SubOrganizationId = null,
            RuleSetId = ruleSetId,
            QueueId = null
        };
    }

    public static PermissionScope ForQueue(QueueId queueId)
    {
        return new PermissionScope
        {
            ScopeType = PermissionScopeType.Queue,
            SubOrganizationId = null,
            RuleSetId = null,
            QueueId = queueId
        };
    }

    public bool Covers(ResourceScope resourceScope)
    {
        return ScopeType switch
        {
            PermissionScopeType.Organization => true,
            PermissionScopeType.SubOrganization => resourceScope.SubOrganizationId == SubOrganizationId,
            PermissionScopeType.RuleSet => resourceScope.RuleSetId == RuleSetId,
            PermissionScopeType.Queue => resourceScope.QueueId == QueueId,
            _ => false
        };
    }

    public PermissionScope NarrowTo(SubOrganizationId subOrganizationId)
    {
        return ScopeType == PermissionScopeType.Organization
            ? ForSubOrganization(subOrganizationId)
            : this;
    }
}
