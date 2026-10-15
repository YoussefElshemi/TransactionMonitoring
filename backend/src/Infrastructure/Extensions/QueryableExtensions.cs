using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Extensions;

public static class QueryableExtensions
{
    public static IQueryable<TEntity> ForUpdate<TEntity>(this IQueryable<TEntity> queryable)
        where TEntity : class
    {
        return queryable
            .IgnoreAutoIncludes()
            .AsTracking();
    }
}
