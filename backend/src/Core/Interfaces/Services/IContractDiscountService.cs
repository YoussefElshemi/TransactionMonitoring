using Core.Models.Discounts;
using Core.ValueObjects;

namespace Core.Interfaces.Services;

public interface IContractDiscountService : IPagedService<ListContractDiscountRequest, ContractDiscount>
{
    Task<ContractDiscount> CreateAsync(CreateContractDiscountRequest createContractDiscountRequest, CancellationToken stoppingToken);
    Task<ContractDiscount> GetAsync(ContractDiscountId contractDiscountId, CancellationToken stoppingToken);
    Task<Exists> ExistsAsync(OrganizationId organizationId, ContractDiscountId contractDiscountId, CancellationToken stoppingToken);
    Task<List<ContractDiscount>> GetInEffectAsync(OrganizationId organizationId, DateTimeOffset asOf, CancellationToken stoppingToken);
    Task DeleteAsync(ContractDiscountId contractDiscountId, CancellationToken stoppingToken);
}
