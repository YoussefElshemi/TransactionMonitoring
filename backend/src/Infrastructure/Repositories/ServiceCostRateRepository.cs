using Core.Enums;
using Core.Interfaces.Repositories;
using Core.Models.Common;
using Core.Models.Revenue;
using Core.ValueObjects;
using Infrastructure.Entities;
using Infrastructure.Expressions;
using Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class ServiceCostRateRepository(
    TransactionMonitoringDbContext context) : PagedRepository<ListServiceCostRateRequest, ServiceCostRateEntity, ServiceCostRate>, IServiceCostRateRepository
{
    public async Task AddAsync(ServiceCostRate serviceCostRate, CancellationToken stoppingToken)
    {
        await context.ServiceCostRates.AddAsync(serviceCostRate.ToEntity(), stoppingToken);
    }

    public async Task<ServiceCostRate> GetAsync(ServiceCostRateId serviceCostRateId, CancellationToken stoppingToken)
    {
        var serviceCostRateEntity = await context.ServiceCostRates
            .FirstAsync(x => x.ServiceCostRateId == serviceCostRateId, stoppingToken);

        return serviceCostRateEntity.ToModel();
    }

    public async Task<Exists> ExistsAsync(BillableResource billableResource, EffectiveFrom effectiveFrom, CancellationToken stoppingToken)
    {
        var from = (DateTimeOffset)effectiveFrom;

        var exists = await context.ServiceCostRates
            .AnyAsync(x => x.BillableResourceId == (int)billableResource &&
                           x.EffectiveFrom == from, stoppingToken);

        return new Exists(exists);
    }

    public async Task<Dictionary<BillableResource, ServiceCostRate>> GetInEffectAsync(DateTimeOffset asOf, CancellationToken stoppingToken)
    {
        var serviceCostRateEntities = await context.ServiceCostRates
            .Where(x => x.EffectiveFrom <= asOf)
            .GroupBy(x => x.BillableResourceId)
            .Select(group => group
                .OrderByDescending(x => x.EffectiveFrom)
                .ThenByDescending(x => x.CreatedAt)
                .First())
            .ToListAsync(stoppingToken);

        return serviceCostRateEntities.ToDictionary(
            x => (BillableResource)x.BillableResourceId,
            x => x.ToModel());
    }

    protected override Task<List<ServiceCostRate>> MapPageAsync(List<ServiceCostRateEntity> items, ListServiceCostRateRequest request, CancellationToken stoppingToken)
    {
        return Task.FromResult(items.ConvertAll(x => x.ToModel()));
    }

    protected override IOrderByExpression<ServiceCostRateEntity> OrderByExpression()
    {
        return new OrderByExpression<ServiceCostRateEntity, DateTimeOffset>(x => x.EffectiveFrom);
    }

    protected override IQueryable<ServiceCostRateEntity> FilterByPagedRequest(ListServiceCostRateRequest listServiceCostRateRequest)
    {
        var queryable = context.ServiceCostRates.AsQueryable();

        if (listServiceCostRateRequest.BillableResource.HasValue)
        {
            queryable = queryable.Where(x => x.BillableResourceId == (int)listServiceCostRateRequest.BillableResource.Value);
        }

        return queryable;
    }

    protected override IQueryable<ServiceCostRateEntity> FilterByDateRange(IQueryable<ServiceCostRateEntity> queryable, DatedRequest datedRequest)
    {
        if (datedRequest.FromDate != DateOnly.MinValue)
        {
            queryable = queryable.Where(x => DateOnly.FromDateTime(x.EffectiveFrom.UtcDateTime) >= datedRequest.FromDate);
        }

        if (datedRequest.ToDate != DateOnly.MaxValue)
        {
            queryable = queryable.Where(x => DateOnly.FromDateTime(x.EffectiveFrom.UtcDateTime) <= datedRequest.ToDate);
        }

        return queryable;
    }
}
