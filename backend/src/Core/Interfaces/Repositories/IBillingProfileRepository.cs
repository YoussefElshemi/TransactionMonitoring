using Core.Models.Billing;
using Core.ValueObjects;

namespace Core.Interfaces.Repositories;

public interface IBillingProfileRepository
{
    Task AddAsync(BillingProfile billingProfile, CancellationToken stoppingToken);
    Task UpdateAsync(BillingProfile billingProfile, CancellationToken stoppingToken);

    Task<BillingProfile?> GetAsync(OrganizationId organizationId, CancellationToken stoppingToken);

    Task<Exists> ExistsAsync(OrganizationId organizationId, CancellationToken stoppingToken);

    Task RecordProviderCustomerAsync(
        OrganizationId organizationId, ProviderCustomerReference providerCustomerReference, CancellationToken stoppingToken);

    Task VerifyTaxIdentifierAsync(OrganizationId organizationId, VerifiedAt verifiedAt, CancellationToken stoppingToken);
}
