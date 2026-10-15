using Core.Models.Billing;
using Core.ValueObjects;

namespace Core.Interfaces.Services;

public interface IBillingProfileService
{
    Task<BillingProfile> SaveAsync(SaveBillingProfileRequest saveBillingProfileRequest, CancellationToken stoppingToken);
    Task<BillingProfile?> FindAsync(OrganizationId organizationId, CancellationToken stoppingToken);
    Task<BillingProfile> GetAsync(OrganizationId organizationId, CancellationToken stoppingToken);
    Task<Exists> ExistsAsync(OrganizationId organizationId, CancellationToken stoppingToken);
    Task<BillingProfile> VerifyTaxIdentifierAsync(OrganizationId organizationId, CancellationToken stoppingToken);

    Task<ProviderCustomerReference> EnsureProviderCustomerAsync(OrganizationId organizationId, CancellationToken stoppingToken);
}
