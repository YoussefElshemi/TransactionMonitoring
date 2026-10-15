using Core.Exceptions;
using Core.Helpers;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Mappers;
using Core.Models.Billing;
using Core.Models.Common;
using Core.ValueObjects;

namespace Core.Services;

public class BillingEntityService(
    IBillingEntityRepository billingEntityRepository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IBillingEntityService
{
    public async Task<BillingEntity> CreateAsync(CreateBillingEntityRequest createBillingEntityRequest, CancellationToken stoppingToken)
    {
        var billingEntity = createBillingEntityRequest.ToModel(timeProvider) with
        {
            TaxIdentifier = Normalise(createBillingEntityRequest.CountryCode, createBillingEntityRequest.TaxIdentifier)
        };

        await billingEntityRepository.AddAsync(billingEntity, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);

        return await GetAsync(billingEntity.BillingEntityId, stoppingToken);
    }

    public async Task<BillingEntity> UpdateAsync(UpdateBillingEntityRequest updateBillingEntityRequest, CancellationToken stoppingToken)
    {
        var existing = await GetAsync(updateBillingEntityRequest.BillingEntityId, stoppingToken);

        var normalised = updateBillingEntityRequest with
        {
            TaxIdentifier = Normalise(
                updateBillingEntityRequest.CountryCode ?? existing.CountryCode,
                updateBillingEntityRequest.TaxIdentifier)
        };

        await billingEntityRepository.UpdateAsync(normalised, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);

        return await GetAsync(updateBillingEntityRequest.BillingEntityId, stoppingToken);
    }

    public Task<BillingEntity> GetAsync(BillingEntityId billingEntityId, CancellationToken stoppingToken)
    {
        return billingEntityRepository.GetAsync(billingEntityId, stoppingToken);
    }

    public Task<Exists> ExistsAsync(BillingEntityId billingEntityId, IncludeDisabled includeDisabled, CancellationToken stoppingToken)
    {
        return billingEntityRepository.ExistsAsync(billingEntityId, includeDisabled, stoppingToken);
    }

    public Task<PagedResponse<BillingEntity>> ListAsync(ListBillingEntityRequest listBillingEntityRequest, CancellationToken stoppingToken)
    {
        return billingEntityRepository.ListAsync(listBillingEntityRequest, stoppingToken);
    }

    public Task<TotalCount> GetCountAsync(ListBillingEntityRequest listBillingEntityRequest, CancellationToken stoppingToken)
    {
        return billingEntityRepository.GetCountAsync(listBillingEntityRequest, stoppingToken);
    }

    public async Task DeleteAsync(BillingEntityId billingEntityId, CancellationToken stoppingToken)
    {
        if (await billingEntityRepository.IsInUseAsync(billingEntityId, stoppingToken))
        {
            throw new BillingEntityInUseException(billingEntityId);
        }

        await billingEntityRepository.DeleteAsync(billingEntityId, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);
    }

    private static TaxIdentifier? Normalise(CountryCode countryCode, TaxIdentifier? taxIdentifier)
    {
        if (!taxIdentifier.HasValue)
        {
            return null;
        }

        var normalised = TaxIdentifierValidator.Normalise(countryCode, taxIdentifier.Value);

        if (!TaxIdentifierValidator.IsWellFormed(countryCode, normalised))
        {
            throw new InvalidTaxIdentifierException(countryCode, taxIdentifier.Value);
        }

        return normalised;
    }
}
