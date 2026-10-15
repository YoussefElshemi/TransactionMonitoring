using Core.Enums;
using Core.Models.Revenue;

namespace Core.Interfaces.Services;

public interface IServiceCostRateService : IPagedService<ListServiceCostRateRequest, ServiceCostRate>
{
    Task<ServiceCostRate> CreateAsync(CreateServiceCostRateRequest createServiceCostRateRequest, CancellationToken stoppingToken);
    Task<Dictionary<BillableResource, ServiceCostRate>> GetInEffectAsync(DateTimeOffset asOf, CancellationToken stoppingToken);
}
