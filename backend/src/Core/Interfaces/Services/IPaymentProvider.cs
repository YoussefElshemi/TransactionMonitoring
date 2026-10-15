using Core.Models.Billing;
using Core.Models.Payments;
using Core.Models.Providers;
using Core.ValueObjects;

namespace Core.Interfaces.Services;

public interface IPaymentProvider
{
    Task<ProviderCustomerReference> EnsureCustomerAsync(BillingProfile billingProfile, CancellationToken stoppingToken);

    Task<HostedSessionUrl> CreateSessionAsync(ProviderSessionRequest providerSessionRequest, CancellationToken stoppingToken);

    Task<List<ProviderPaymentMethod>> ListPaymentMethodsAsync(
        ProviderCustomerReference providerCustomerReference, CancellationToken stoppingToken);

    // The idempotency key is derived from the invoice and attempt, so a retried job cannot charge twice.
    Task<ProviderChargeResult> ChargeAsync(ProviderChargeRequest providerChargeRequest, CancellationToken stoppingToken);

    // Takes the raw bytes because the signature is over them and JSON round-tripping won't reproduce them. Null means unverified.
    ProviderEvent? ReadEvent(string payload, string? signature);
}
