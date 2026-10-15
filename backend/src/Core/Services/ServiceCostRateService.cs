using Core.Enums;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Mappers;
using Core.Models.Common;
using Core.Models.Revenue;
using Core.ValueObjects;

namespace Core.Services;

public class ServiceCostRateService(
    IServiceCostRateRepository serviceCostRateRepository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IServiceCostRateService
{
    public async Task<ServiceCostRate> CreateAsync(CreateServiceCostRateRequest createServiceCostRateRequest, CancellationToken stoppingToken)
    {
        var serviceCostRate = createServiceCostRateRequest.ToModel(timeProvider);

        await serviceCostRateRepository.AddAsync(serviceCostRate, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);

        return await serviceCostRateRepository.GetAsync(serviceCostRate.ServiceCostRateId, stoppingToken);
    }

    public Task<Dictionary<BillableResource, ServiceCostRate>> GetInEffectAsync(DateTimeOffset asOf, CancellationToken stoppingToken)
    {
        return serviceCostRateRepository.GetInEffectAsync(asOf, stoppingToken);
    }

    public Task<PagedResponse<ServiceCostRate>> ListAsync(ListServiceCostRateRequest listServiceCostRateRequest, CancellationToken stoppingToken)
    {
        return serviceCostRateRepository.ListAsync(listServiceCostRateRequest, stoppingToken);
    }

    public Task<TotalCount> GetCountAsync(ListServiceCostRateRequest listServiceCostRateRequest, CancellationToken stoppingToken)
    {
        return serviceCostRateRepository.GetCountAsync(listServiceCostRateRequest, stoppingToken);
    }
}
