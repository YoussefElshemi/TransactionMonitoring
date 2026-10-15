using Core.Models.Permissions;
using Infrastructure.Entities;

namespace Infrastructure.Expressions;

public static class PermissionFilterExpressions
{
    public static IQueryable<CaseEntity> ApplyPermissionFilter(this IQueryable<CaseEntity> queryable, PermissionFilter? filter)
    {
        if (filter is null)
        {
            return queryable;
        }

        var queueIds = filter.QueueIds.ConvertAll(x => (Guid)x);

        return queryable.Where(x => x.QueueId.HasValue && queueIds.Contains(x.QueueId.Value));
    }

    public static IQueryable<RuleEntity> ApplyPermissionFilter(this IQueryable<RuleEntity> queryable, PermissionFilter? filter)
    {
        if (filter is null)
        {
            return queryable;
        }

        var ruleSetIds = filter.RuleSetIds.ConvertAll(x => (Guid)x);

        return queryable.Where(x => ruleSetIds.Contains(x.RuleSetId));
    }

    public static IQueryable<RuleSetEntity> ApplyPermissionFilter(this IQueryable<RuleSetEntity> queryable, PermissionFilter? filter)
    {
        if (filter is null)
        {
            return queryable;
        }

        var ruleSetIds = filter.RuleSetIds.ConvertAll(x => (Guid)x);

        return queryable.Where(x => ruleSetIds.Contains(x.RuleSetId));
    }

    public static IQueryable<QueueEntity> ApplyPermissionFilter(this IQueryable<QueueEntity> queryable, PermissionFilter? filter)
    {
        if (filter is null)
        {
            return queryable;
        }

        var queueIds = filter.QueueIds.ConvertAll(x => (Guid)x);

        return queryable.Where(x => queueIds.Contains(x.QueueId));
    }
}
