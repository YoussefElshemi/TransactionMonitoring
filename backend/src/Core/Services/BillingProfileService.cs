using Core.Exceptions;
using Core.Helpers;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Mappers;
using Core.Models.Billing;
using Core.ValueObjects;

namespace Core.Services;

public class BillingProfileService(
    IBillingProfileRepository billingProfileRepository,
    IPaymentProvider paymentProvider,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IBillingProfileService
{
    public async Task<BillingProfile> SaveAsync(SaveBillingProfileRequest saveBillingProfileRequest, CancellationToken stoppingToken)
    {
        var normalised = saveBillingProfileRequest with
        {
            TaxIdentifier = Normalise(saveBillingProfileRequest.CountryCode, saveBillingProfileRequest.TaxIdentifier)
        };

        var existing = await billingProfileRepository.GetAsync(saveBillingProfileRequest.OrganizationId, stoppingToken);

        if (existing is null)
        {
            await billingProfileRepository.AddAsync(normalised.ToModel(timeProvider), stoppingToken);
        }
        else
        {
            await billingProfileRepository.UpdateAsync(normalised.ToModel(existing, timeProvider), stoppingToken);
        }

        await unitOfWork.SaveChangesAsync(stoppingToken);

        return await GetAsync(saveBillingProfileRequest.OrganizationId, stoppingToken);
    }

    public Task<BillingProfile?> FindAsync(OrganizationId organizationId, CancellationToken stoppingToken)
    {
        return billingProfileRepository.GetAsync(organizationId, stoppingToken);
    }

    public async Task<BillingProfile> GetAsync(OrganizationId organizationId, CancellationToken stoppingToken)
    {
        return await billingProfileRepository.GetAsync(organizationId, stoppingToken)
               ?? throw new NoBillingProfileException(organizationId);
    }

    public Task<Exists> ExistsAsync(OrganizationId organizationId, CancellationToken stoppingToken)
    {
        return billingProfileRepository.ExistsAsync(organizationId, stoppingToken);
    }

    public async Task<BillingProfile> VerifyTaxIdentifierAsync(OrganizationId organizationId, CancellationToken stoppingToken)
    {
        var billingProfile = await GetAsync(organizationId, stoppingToken);

        if (!billingProfile.TaxIdentifier.HasValue)
        {
            throw new NoBillingProfileException(organizationId);
        }

        if (!TaxIdentifierValidator.IsWellFormed(billingProfile.CountryCode, billingProfile.TaxIdentifier.Value))
        {
            throw new InvalidTaxIdentifierException(billingProfile.CountryCode, billingProfile.TaxIdentifier.Value);
        }

        await billingProfileRepository.VerifyTaxIdentifierAsync(
            organizationId, new VerifiedAt(timeProvider.GetUtcNow()), stoppingToken);

        await unitOfWork.SaveChangesAsync(stoppingToken);

        return await GetAsync(organizationId, stoppingToken);
    }

    public async Task<ProviderCustomerReference> EnsureProviderCustomerAsync(OrganizationId organizationId, CancellationToken stoppingToken)
    {
        var billingProfile = await GetAsync(organizationId, stoppingToken);

        if (billingProfile.ProviderCustomerReference.HasValue)
        {
            return billingProfile.ProviderCustomerReference.Value;
        }

        var providerCustomerReference = await paymentProvider.EnsureCustomerAsync(billingProfile, stoppingToken);

        await billingProfileRepository.RecordProviderCustomerAsync(organizationId, providerCustomerReference, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);

        return providerCustomerReference;
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
