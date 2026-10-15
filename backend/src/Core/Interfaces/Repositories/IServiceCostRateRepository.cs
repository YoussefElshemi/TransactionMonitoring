using Core.Enums;
using Core.Models.Revenue;
using Core.ValueObjects;

namespace Core.Interfaces.Repositories;

public interface IServiceCostRateRepository : IPagedRepository<ListServiceCostRateRequest, ServiceCostRate>
{
    Task AddAsync(ServiceCostRate serviceCostRate, CancellationToken stoppingToken);
    Task<ServiceCostRate> GetAsync(ServiceCostRateId serviceCostRateId, CancellationToken stoppingToken);
    Task<Exists> ExistsAsync(BillableResource billableResource, EffectiveFrom effectiveFrom, CancellationToken stoppingToken);

    Task<Dictionary<BillableResource, ServiceCostRate>> GetInEffectAsync(DateTimeOffset asOf, CancellationToken stoppingToken);
}
