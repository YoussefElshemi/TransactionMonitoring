using Core.Exceptions;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Mappers;
using Core.Models.Common;
using Core.Models.Discounts;
using Core.ValueObjects;

namespace Core.Services;

public class ContractDiscountService(
    IContractDiscountRepository contractDiscountRepository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IContractDiscountService
{
    public async Task<ContractDiscount> CreateAsync(CreateContractDiscountRequest createContractDiscountRequest, CancellationToken stoppingToken)
    {
        var contractDiscount = createContractDiscountRequest.ToModel(timeProvider);

        if (contractDiscount.EffectiveTo is not null && contractDiscount.EffectiveTo.Value <= (DateTimeOffset)contractDiscount.EffectiveFrom)
        {
            throw new InvalidPriceStructureException("a discount cannot end before it starts.");
        }

        await contractDiscountRepository.AddAsync(contractDiscount, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);

        return await GetAsync(contractDiscount.ContractDiscountId, stoppingToken);
    }

    public Task<ContractDiscount> GetAsync(ContractDiscountId contractDiscountId, CancellationToken stoppingToken)
    {
        return contractDiscountRepository.GetAsync(contractDiscountId, stoppingToken);
    }

    public Task<Exists> ExistsAsync(OrganizationId organizationId, ContractDiscountId contractDiscountId, CancellationToken stoppingToken)
    {
        return contractDiscountRepository.ExistsAsync(organizationId, contractDiscountId, stoppingToken);
    }

    public Task<List<ContractDiscount>> GetInEffectAsync(OrganizationId organizationId, DateTimeOffset asOf, CancellationToken stoppingToken)
    {
        return contractDiscountRepository.GetInEffectAsync(organizationId, asOf, stoppingToken);
    }

    public Task<PagedResponse<ContractDiscount>> ListAsync(ListContractDiscountRequest listContractDiscountRequest, CancellationToken stoppingToken)
    {
        return contractDiscountRepository.ListAsync(listContractDiscountRequest, stoppingToken);
    }

    public Task<TotalCount> GetCountAsync(ListContractDiscountRequest listContractDiscountRequest, CancellationToken stoppingToken)
    {
        return contractDiscountRepository.GetCountAsync(listContractDiscountRequest, stoppingToken);
    }

    public async Task DeleteAsync(ContractDiscountId contractDiscountId, CancellationToken stoppingToken)
    {
        var contractDiscount = await GetAsync(contractDiscountId, stoppingToken);
        var now = timeProvider.GetUtcNow();

        if (contractDiscount.EffectiveFrom <= now)
        {
            var endsAt = contractDiscount.EffectiveTo is null || contractDiscount.EffectiveTo.Value > now
                ? now
                : contractDiscount.EffectiveTo.Value;

            await contractDiscountRepository.EndAsync(contractDiscountId, new EffectiveTo(endsAt), stoppingToken);
        }
        else
        {
            await contractDiscountRepository.DeleteAsync(contractDiscountId, stoppingToken);
        }

        await unitOfWork.SaveChangesAsync(stoppingToken);
    }
}
