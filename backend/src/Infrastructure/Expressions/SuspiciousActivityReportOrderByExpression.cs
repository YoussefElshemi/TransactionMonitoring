using Core.Extensions;
using Infrastructure.Entities;

namespace Infrastructure.Expressions;

public class SuspiciousActivityReportOrderByExpression : IOrderByExpression<SuspiciousActivityReportEntity>
{
    public IQueryable<SuspiciousActivityReportEntity> ApplyOrderBy(
        IQueryable<SuspiciousActivityReportEntity> queryable,
        bool ascending = false)
    {
        return queryable
            .OrderBy(x => SuspiciousActivityReportStatusExtensions.TerminalStatusIds.Contains(x.StatusId))
            .ThenBy(x => SuspiciousActivityReportStatusExtensions.TerminalStatusIds.Contains(x.StatusId)
                ? (DateTimeOffset?)null
                : x.DueAt)
            .ThenByDescending(x => x.DueAt)
            .ThenBy(x => x.CreatedAt);
    }
}
