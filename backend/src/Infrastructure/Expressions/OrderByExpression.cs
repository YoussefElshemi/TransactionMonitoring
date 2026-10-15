using System.Linq.Expressions;

namespace Infrastructure.Expressions;

public interface IOrderByExpression<TEntity>
{
    IQueryable<TEntity> ApplyOrderBy(IQueryable<TEntity> queryable, bool ascending = false);
}

public class OrderByExpression<TEntity, TKey>(
    Expression<Func<TEntity, TKey>> keySelector,
    bool ascendingByDefault = false,
    Expression<Func<TEntity, DateTimeOffset>>? tieBreakSelector = null) : IOrderByExpression<TEntity>
{
    public IQueryable<TEntity> ApplyOrderBy(IQueryable<TEntity> queryable, bool ascending = false)
    {
        var ordered = ascending || ascendingByDefault
            ? queryable.OrderBy(keySelector)
            : queryable.OrderByDescending(keySelector);

        // A unique tiebreaker: with ties Postgres may order differently between the count and the page, or between pages.
        return tieBreakSelector is null ? ordered : ordered.ThenBy(tieBreakSelector);
    }
}
