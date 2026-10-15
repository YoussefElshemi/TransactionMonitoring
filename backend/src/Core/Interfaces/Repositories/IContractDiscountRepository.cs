using Core.Models.Discounts;
using Core.ValueObjects;

namespace Core.Interfaces.Repositories;

public interface IContractDiscountRepository : IPagedRepository<ListContractDiscountRequest, ContractDiscount>
{
    Task AddAsync(ContractDiscount contractDiscount, CancellationToken stoppingToken);
    Task<ContractDiscount> GetAsync(ContractDiscountId contractDiscountId, CancellationToken stoppingToken);
    Task<Exists> ExistsAsync(OrganizationId organizationId, ContractDiscountId contractDiscountId, CancellationToken stoppingToken);

    Task<List<ContractDiscount>> GetInEffectAsync(OrganizationId organizationId, DateTimeOffset asOf, CancellationToken stoppingToken);

    Task EndAsync(ContractDiscountId contractDiscountId, EffectiveTo effectiveTo, CancellationToken stoppingToken);
    Task DeleteAsync(ContractDiscountId contractDiscountId, CancellationToken stoppingToken);
}
