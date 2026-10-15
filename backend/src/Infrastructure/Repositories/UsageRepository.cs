using Core.Enums;
using Core.Interfaces.Repositories;
using Core.Models.Common;
using Core.Models.Revenue;
using Core.Models.Usages;
using Core.ValueObjects;
using Infrastructure.Entities;
using Infrastructure.Expressions;
using Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class UsageRepository(TransactionMonitoringDbContext context) : PagedRepository<ListUsageRequest, UsageEntity, Usage>, IUsageRepository
{
    public async Task AddAsync(Usage usage, CancellationToken stoppingToken)
    {
        var usageEntity = usage.ToEntity();
        await context.Usages.AddAsync(usageEntity, stoppingToken);
    }

    public async Task<Usage> GetAsync(UsageId usageId, CancellationToken stoppingToken)
    {
        var usageEntity = await context.Usages
            .FirstAsync(x => x.UsageId == usageId, stoppingToken);

        return usageEntity.ToModel();
    }

    public async Task<Exists> ExistsAsync(OrganizationId organizationId, UsageId usageId, CancellationToken stoppingToken)
    {
        var exists = await context.Usages
            .AnyAsync(x => x.OrganizationId == organizationId &&
                           x.UsageId == usageId, stoppingToken);

        return new Exists(exists);
    }

    public async Task<Sum> GetSumAsync(ListUsageRequest listUsageRequest, CancellationToken stoppingToken)
    {
        var queryable = FilterByPagedRequest(listUsageRequest);

        var count = await queryable
            .Where(x => x.OrganizationId == listUsageRequest.OrganizationId &&
                        DateOnly.FromDateTime(x.CreatedAt.UtcDateTime) >= listUsageRequest.FromDate &&
                        DateOnly.FromDateTime(x.CreatedAt.UtcDateTime) <= listUsageRequest.ToDate)
            .SumAsync(x => x.Count, stoppingToken);

        return new Sum(count);
    }

    public async Task<Dictionary<BillableResource, Count>> GroupByResourceAsync(ListUsageRequest listUsageRequest, CancellationToken stoppingToken)
    {
        var queryable = FilterByDateRange(FilterByPagedRequest(listUsageRequest), listUsageRequest)
            .GroupBy(x => x.BillableResourceId)
            .Select(group => new
            {
                BillableResourceId = group.Key,
                Count = group.Sum(x => x.Count)
            });

        var grouped = await queryable.ToListAsync(stoppingToken);

        return grouped.ToDictionary(
            x => (BillableResource)x.BillableResourceId,
            x => new Count(x.Count));
    }

    public Task<Dictionary<DateOnly, int>> GroupByDateAsync(ListUsageRequest listUsageRequest, CancellationToken stoppingToken)
    {
        var queryable = FilterByDateRange(FilterByPagedRequest(listUsageRequest), listUsageRequest)
            .GroupBy(x => DateOnly.FromDateTime(x.CreatedAt.UtcDateTime))
            .Select(group => new
            {
                Date = group.Key,
                Count = group.Sum(x => x.Count)
            });

        return queryable
            .OrderBy(x => x.Date)
            .ToDictionaryAsync(
                x => x.Date,
                x => x.Count,
                stoppingToken
            );
    }

    public async Task<List<OrganizationResourceUsage>> GroupByOrganizationAndResourceAsync(
        PeriodStart periodStart, PeriodEnd periodEnd, CancellationToken stoppingToken)
    {
        var from = (DateTimeOffset)periodStart;
        var to = (DateTimeOffset)periodEnd;

        var grouped = await context.Usages
            .Where(x => x.CreatedAt >= from && x.CreatedAt <= to)
            .GroupBy(x => new { x.OrganizationId, x.BillableResourceId })
            .Select(group => new
            {
                group.Key.OrganizationId,
                group.Key.BillableResourceId,
                Count = group.Sum(x => x.Count)
            })
            .ToListAsync(stoppingToken);

        return grouped.ConvertAll(x => new OrganizationResourceUsage
        {
            OrganizationId = new OrganizationId(x.OrganizationId),
            BillableResource = (BillableResource)x.BillableResourceId,
            Count = new Count(x.Count)
        });
    }

    public Task<Dictionary<Guid, Count>> SumByOrganizationAsync(
        PeriodStart periodStart, PeriodEnd periodEnd, CancellationToken stoppingToken)
    {
        var from = (DateTimeOffset)periodStart;
        var to = (DateTimeOffset)periodEnd;

        return context.Usages
            .Where(x => x.CreatedAt >= from && x.CreatedAt <= to)
            .GroupBy(x => x.OrganizationId)
            .Select(group => new
            {
                OrganizationId = group.Key,
                Count = group.Sum(x => x.Count)
            })
            .ToDictionaryAsync(
                x => x.OrganizationId,
                x => new Count(x.Count),
                stoppingToken);
    }

    public Task<Dictionary<Guid, DateTimeOffset>> GetLastMeteredAtAsync(CancellationToken stoppingToken)
    {
        return context.Usages
            .GroupBy(x => x.OrganizationId)
            .Select(group => new
            {
                OrganizationId = group.Key,
                LastMeteredAt = group.Max(x => x.CreatedAt)
            })
            .ToDictionaryAsync(
                x => x.OrganizationId,
                x => x.LastMeteredAt,
                stoppingToken);
    }

    protected override Task<List<Usage>> MapPageAsync(List<UsageEntity> items, ListUsageRequest request, CancellationToken stoppingToken)
    {
        return Task.FromResult(items.ConvertAll(x => x.ToModel()));
    }

    protected override IOrderByExpression<UsageEntity> OrderByExpression()
    {
        return new OrderByExpression<UsageEntity, DateTimeOffset>(x => x.UpdatedAt);
    }

    protected override IQueryable<UsageEntity> FilterByPagedRequest(ListUsageRequest listUsageRequest)
    {
        var queryable = context.Usages
            .Where(x => x.OrganizationId == listUsageRequest.OrganizationId);

        if (listUsageRequest.Resource.HasValue)
        {
            queryable = queryable
                .Where(x => EF.Functions.ILike(x.Resource, $"%{listUsageRequest.Resource}%"));
        }

        return queryable;
    }

    protected override IQueryable<UsageEntity> FilterByDateRange(IQueryable<UsageEntity> queryable, DatedRequest datedRequest)
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