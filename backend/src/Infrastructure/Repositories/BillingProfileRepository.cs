using Core.Interfaces.Repositories;
using Core.Models.Billing;
using Core.ValueObjects;
using Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class BillingProfileRepository(
    TransactionMonitoringDbContext context,
    TimeProvider timeProvider) : IBillingProfileRepository
{
    public async Task AddAsync(BillingProfile billingProfile, CancellationToken stoppingToken)
    {
        await context.BillingProfiles.AddAsync(billingProfile.ToEntity(), stoppingToken);
    }

    public async Task UpdateAsync(BillingProfile billingProfile, CancellationToken stoppingToken)
    {
        var billingProfileEntity = await context.BillingProfiles
            .FirstAsync(x => x.OrganizationId == billingProfile.OrganizationId && !x.IsDeleted, stoppingToken);

        billingProfileEntity.BillingEntityId = billingProfile.BillingEntityId;
        billingProfileEntity.LegalName = billingProfile.LegalName;
        billingProfileEntity.ContactName = billingProfile.ContactName;
        billingProfileEntity.ContactEmail = billingProfile.ContactEmail;
        billingProfileEntity.Address = billingProfile.Address;
        billingProfileEntity.CountryCode = billingProfile.CountryCode;
        billingProfileEntity.TaxIdentifier = billingProfile.TaxIdentifier.HasValue ? billingProfile.TaxIdentifier.Value : null;
        billingProfileEntity.TaxIdentifierVerifiedAt = billingProfile.TaxIdentifierVerifiedAt.HasValue ? billingProfile.TaxIdentifierVerifiedAt.Value : null;
        billingProfileEntity.CurrencyId = (int)billingProfile.Currency;
        billingProfileEntity.PaymentTerms = billingProfile.PaymentTerms;
        billingProfileEntity.UpdatedByUserId = billingProfile.UpdatedByUserId.HasValue ? billingProfile.UpdatedByUserId.Value : null;
        billingProfileEntity.UpdatedAt = timeProvider.GetUtcNow();

        context.BillingProfiles.Update(billingProfileEntity);
    }

    public async Task<BillingProfile?> GetAsync(OrganizationId organizationId, CancellationToken stoppingToken)
    {
        var billingProfileEntity = await context.BillingProfiles
            .FirstOrDefaultAsync(x => x.OrganizationId == organizationId && !x.IsDeleted, stoppingToken);

        return billingProfileEntity?.ToModel();
    }

    public async Task<Exists> ExistsAsync(OrganizationId organizationId, CancellationToken stoppingToken)
    {
        var exists = await context.BillingProfiles
            .AnyAsync(x => x.OrganizationId == organizationId && !x.IsDeleted, stoppingToken);

        return new Exists(exists);
    }

    public async Task RecordProviderCustomerAsync(
        OrganizationId organizationId, ProviderCustomerReference providerCustomerReference, CancellationToken stoppingToken)
    {
        var billingProfileEntity = await context.BillingProfiles
            .FirstAsync(x => x.OrganizationId == organizationId && !x.IsDeleted, stoppingToken);

        billingProfileEntity.ProviderCustomerReference = providerCustomerReference;
        billingProfileEntity.UpdatedAt = timeProvider.GetUtcNow();

        context.BillingProfiles.Update(billingProfileEntity);
    }

    public async Task VerifyTaxIdentifierAsync(OrganizationId organizationId, VerifiedAt verifiedAt, CancellationToken stoppingToken)
    {
        var billingProfileEntity = await context.BillingProfiles
            .FirstAsync(x => x.OrganizationId == organizationId && !x.IsDeleted, stoppingToken);

        billingProfileEntity.TaxIdentifierVerifiedAt = verifiedAt;
        billingProfileEntity.UpdatedAt = timeProvider.GetUtcNow();

        context.BillingProfiles.Update(billingProfileEntity);
    }
}
