using Core.Models.Common;
using Core.ValueObjects;
using Infrastructure.Expressions;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public abstract class PagedRepository<TRequest, TEntity, TModel>
    where TRequest : PagedRequest
    where TEntity : class
    where TModel : class
{
    protected abstract Task<List<TModel>> MapPageAsync(List<TEntity> items, TRequest request, CancellationToken stoppingToken);

    protected abstract IOrderByExpression<TEntity> OrderByExpression();

    protected abstract IQueryable<TEntity> FilterByPagedRequest(TRequest request);
    protected abstract IQueryable<TEntity> FilterByDateRange(IQueryable<TEntity> queryable, DatedRequest datedRequest);

    public async Task<TotalCount> GetCountAsync(TRequest pagedRequest, CancellationToken stoppingToken)
    {
        var queryable = Filter(pagedRequest);
        var count = await queryable.CountAsync(stoppingToken);

        return new TotalCount(count);
    }

    public async Task<PagedResponse<TModel>> ListAsync(TRequest pagedRequest, CancellationToken stoppingToken)
    {
        var queryable = Filter(pagedRequest);

        var totalCount = await GetCountAsync(pagedRequest, stoppingToken);
        var pageCount = pagedRequest.GetPageCount(totalCount);

        queryable = OrderByExpression().ApplyOrderBy(queryable);

        var items = await queryable
            .Skip((pagedRequest.PageNumber - 1) * pagedRequest.PageSize)
            .Take(pagedRequest.PageSize)
            .ToListAsync(stoppingToken);

        var data = await MapPageAsync(items, pagedRequest, stoppingToken);

        return new PagedResponse<TModel>
        {
            PageNumber = pagedRequest.PageNumber,
            PageSize = pagedRequest.PageSize,
            PageCount = pageCount,
            TotalCount = totalCount,
            HasMore = new HasMore(pagedRequest.PageNumber * pagedRequest.PageSize < totalCount),
            Data = data
        };
    }

    protected IQueryable<TEntity> Filter(TRequest request)
    {
        return FilterByDateRange(FilterByPagedRequest(request), request);
    }
}