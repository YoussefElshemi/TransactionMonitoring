using Core.Interfaces.Repositories;
using Core.Models.Common;
using Core.Models.RateLimitOverrides;
using Core.ValueObjects;
using Infrastructure.Entities;
using Infrastructure.Expressions;
using Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class RateLimitOverrideRepository(
    TransactionMonitoringDbContext context,
    TimeProvider timeProvider)
    : PagedRepository<ListRateLimitOverrideRequest, RateLimitOverrideEntity, RateLimitOverride>, IRateLimitOverrideRepository
{
    public async Task AddAsync(RateLimitOverride rateLimitOverride, CancellationToken stoppingToken)
    {
        var rateLimitOverrideEntity = rateLimitOverride.ToEntity();
        await context.RateLimitOverrides.AddAsync(rateLimitOverrideEntity, stoppingToken);
    }

    public async Task<RateLimitOverride> GetAsync(RateLimitOverrideId rateLimitOverrideId, CancellationToken stoppingToken)
    {
        var rateLimitOverrideEntity = await context.RateLimitOverrides
            .FirstAsync(x => x.RateLimitOverrideId == rateLimitOverrideId && !x.IsDeleted, stoppingToken);

        return rateLimitOverrideEntity.ToModel();
    }

    public async Task<Exists> ExistsAsync(OrganizationId organizationId, RateLimitOverrideId rateLimitOverrideId, CancellationToken stoppingToken)
    {
        var exists = await context.RateLimitOverrides
            .AnyAsync(x => x.OrganizationId == organizationId &&
                           x.RateLimitOverrideId == rateLimitOverrideId &&
                           !x.IsDeleted, stoppingToken);

        return new Exists(exists);
    }

    public async Task<IReadOnlyCollection<RateLimitOverride>> ListActiveAsync(CancellationToken stoppingToken)
    {
        var now = timeProvider.GetUtcNow();

        var rateLimitOverrideEntities = await context.RateLimitOverrides
            .Where(x => !x.IsDeleted && x.ExpiresAt > now)
            .ToListAsync(stoppingToken);

        return rateLimitOverrideEntities.ConvertAll(x => x.ToModel());
    }

    public async Task DeleteAsync(RateLimitOverrideId rateLimitOverrideId, CancellationToken stoppingToken)
    {
        var rateLimitOverrideEntity = await context.RateLimitOverrides
            .FirstAsync(x => x.RateLimitOverrideId == rateLimitOverrideId, stoppingToken);

        rateLimitOverrideEntity.IsDeleted = true;
        rateLimitOverrideEntity.DeletedAt = timeProvider.GetUtcNow();
        rateLimitOverrideEntity.UpdatedAt = timeProvider.GetUtcNow();

        context.RateLimitOverrides.Update(rateLimitOverrideEntity);
    }

    protected override Task<List<RateLimitOverride>> MapPageAsync(List<RateLimitOverrideEntity> items, ListRateLimitOverrideRequest request, CancellationToken stoppingToken)
    {
        return Task.FromResult(items.ConvertAll(x => x.ToModel()));
    }

    protected override IOrderByExpression<RateLimitOverrideEntity> OrderByExpression()
    {
        return new OrderByExpression<RateLimitOverrideEntity, DateTimeOffset>(x => x.CreatedAt);
    }

    protected override IQueryable<RateLimitOverrideEntity> FilterByPagedRequest(ListRateLimitOverrideRequest listRateLimitOverrideRequest)
    {
        var queryable = context.RateLimitOverrides
            .Where(x => x.OrganizationId == listRateLimitOverrideRequest.OrganizationId && !x.IsDeleted);

        if (!listRateLimitOverrideRequest.IncludeExpired)
        {
            var now = timeProvider.GetUtcNow();
            queryable = queryable.Where(x => x.ExpiresAt > now);
        }

        if (listRateLimitOverrideRequest.Pool.HasValue)
        {
            queryable = queryable
                .Where(x => x.PoolId == (int)listRateLimitOverrideRequest.Pool.Value);
        }

        return queryable;
    }

    protected override IQueryable<RateLimitOverrideEntity> FilterByDateRange(IQueryable<RateLimitOverrideEntity> queryable, DatedRequest datedRequest)
    {
        if (datedRequest.FromDate != DateOnly.MinValue)
        {
            queryable = queryable
                .Where(x => DateOnly.FromDateTime(x.CreatedAt.UtcDateTime) >= datedRequest.FromDate);
        }

        if (datedRequest.ToDate != DateOnly.MaxValue)
        {
            queryable = queryable
                .Where(x => DateOnly.FromDateTime(x.CreatedAt.UtcDateTime) <= datedRequest.ToDate);
        }

        return queryable;
    }
}
