using Core.Enums;
using Core.Models.Statistics;
using Core.Models.Usages;
using Core.ValueObjects;

namespace Core.Interfaces.Services;

public interface IUsageService : IPagedService<ListUsageRequest, Usage>
{
    Task<Usage> CreateAsync(CreateUsageRequest createUsageRequest, CancellationToken stoppingToken);

    Task RecordBillableAsync(CreateUsageRequest createUsageRequest, CancellationToken stoppingToken);
    Task<Usage> GetAsync(UsageId usageId, CancellationToken stoppingToken);
    Task<Exists> ExistsAsync(OrganizationId organizationId, UsageId usageId, CancellationToken stoppingToken);
    Task<Sum> SumByDateAsync(SumUsageByDateRequest sumUsageByDateRequest, CancellationToken stoppingToken);
    Task<Dictionary<BillableResource, Count>> GroupByResourceAsync(SumUsageByDateRequest sumUsageByDateRequest, CancellationToken stoppingToken);
    Task<Dictionary<DateOnly, int>> GroupByDateAsync(StatisticRequest statisticRequest, CancellationToken stoppingToken);
}