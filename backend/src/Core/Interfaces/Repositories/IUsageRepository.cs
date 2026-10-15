using Core.Enums;
using Core.Models.Revenue;
using Core.Models.Usages;
using Core.ValueObjects;

namespace Core.Interfaces.Repositories;

public interface IUsageRepository : IPagedRepository<ListUsageRequest, Usage>
{
    Task AddAsync(Usage usage, CancellationToken stoppingToken);
    Task<Usage> GetAsync(UsageId usageId, CancellationToken stoppingToken);
    Task<Exists> ExistsAsync(OrganizationId organizationId, UsageId usageId, CancellationToken stoppingToken);
    Task<Sum> GetSumAsync(ListUsageRequest listUsageRequest, CancellationToken stoppingToken);

    Task<Dictionary<BillableResource, Count>> GroupByResourceAsync(ListUsageRequest listUsageRequest, CancellationToken stoppingToken);
    Task<Dictionary<DateOnly, int>> GroupByDateAsync(ListUsageRequest listUsageRequest, CancellationToken stoppingToken);

    Task<List<OrganizationResourceUsage>> GroupByOrganizationAndResourceAsync(PeriodStart periodStart, PeriodEnd periodEnd, CancellationToken stoppingToken);

    Task<Dictionary<Guid, Count>> SumByOrganizationAsync(PeriodStart periodStart, PeriodEnd periodEnd, CancellationToken stoppingToken);

    Task<Dictionary<Guid, DateTimeOffset>> GetLastMeteredAtAsync(CancellationToken stoppingToken);
}