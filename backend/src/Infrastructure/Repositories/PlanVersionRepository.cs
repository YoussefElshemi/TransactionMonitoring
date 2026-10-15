using Core.Interfaces.Repositories;
using Core.Models.Common;
using Core.Models.Plans;
using Core.ValueObjects;
using Infrastructure.Entities;
using Infrastructure.Expressions;
using Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class PlanVersionRepository(TransactionMonitoringDbContext context)
    : PagedRepository<ListPlanVersionRequest, PlanVersionEntity, PlanVersion>, IPlanVersionRepository
{
    public async Task AddAsync(PlanVersion planVersion, CancellationToken stoppingToken)
    {
        var planVersionEntity = planVersion.ToEntity();
        await context.PlanVersions.AddAsync(planVersionEntity, stoppingToken);

        foreach (var price in planVersion.Prices)
        {
            await context.PlanPrices.AddAsync(price.ToEntity(), stoppingToken);

            foreach (var tier in price.Tiers)
            {
                await context.PlanPriceTiers.AddAsync(tier.ToEntity(), stoppingToken);
            }
        }
    }

    public async Task<PlanVersion> GetAsync(PlanVersionId planVersionId, CancellationToken stoppingToken)
    {
        var planVersionEntity = await context.PlanVersions
            .FirstAsync(x => x.PlanVersionId == planVersionId, stoppingToken);

        return planVersionEntity.ToModel();
    }

    public async Task<PlanVersion?> GetCurrentAsync(PlanId planId, CancellationToken stoppingToken)
    {
        var planVersionEntity = await context.PlanVersions
            .Where(x => x.PlanId == planId)
            .OrderByDescending(x => x.VersionNumber)
            .FirstOrDefaultAsync(stoppingToken);

        return planVersionEntity?.ToModel();
    }

    public async Task<Exists> ExistsAsync(PlanVersionId planVersionId, CancellationToken stoppingToken)
    {
        var exists = await context.PlanVersions
            .AnyAsync(x => x.PlanVersionId == planVersionId, stoppingToken);

        return new Exists(exists);
    }

    public async Task<VersionNumber> GetNextVersionNumberAsync(PlanId planId, CancellationToken stoppingToken)
    {
        var highest = await context.PlanVersions
            .Where(x => x.PlanId == planId)
            .MaxAsync(x => (int?)x.VersionNumber, stoppingToken);

        return new VersionNumber((highest ?? 0) + 1);
    }

    protected override Task<List<PlanVersion>> MapPageAsync(List<PlanVersionEntity> items, ListPlanVersionRequest request, CancellationToken stoppingToken)
    {
        return Task.FromResult(items.ConvertAll(x => x.ToModel()));
    }

    protected override IOrderByExpression<PlanVersionEntity> OrderByExpression()
    {
        return new OrderByExpression<PlanVersionEntity, int>(x => x.VersionNumber);
    }

    protected override IQueryable<PlanVersionEntity> FilterByPagedRequest(ListPlanVersionRequest listPlanVersionRequest)
    {
        return context.PlanVersions
            .Where(x => x.PlanId == listPlanVersionRequest.PlanId);
    }

    protected override IQueryable<PlanVersionEntity> FilterByDateRange(IQueryable<PlanVersionEntity> queryable, DatedRequest datedRequest)
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
