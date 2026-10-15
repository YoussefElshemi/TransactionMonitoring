using Core.Exceptions;
using Core.Interfaces.Services;
using Core.Models.Billing;
using Core.Models.Payments;
using Core.Models.Providers;
using Core.ValueObjects;

namespace Infrastructure.Services;

public class DisabledPaymentProvider : IPaymentProvider
{
    public Task<ProviderCustomerReference> EnsureCustomerAsync(BillingProfile billingProfile, CancellationToken stoppingToken)
    {
        throw new PaymentProviderUnavailableException();
    }

    public Task<HostedSessionUrl> CreateSessionAsync(ProviderSessionRequest providerSessionRequest, CancellationToken stoppingToken)
    {
        throw new PaymentProviderUnavailableException();
    }

    public Task<List<ProviderPaymentMethod>> ListPaymentMethodsAsync(
        ProviderCustomerReference providerCustomerReference, CancellationToken stoppingToken)
    {
        return Task.FromResult(new List<ProviderPaymentMethod>());
    }

    public Task<ProviderChargeResult> ChargeAsync(ProviderChargeRequest providerChargeRequest, CancellationToken stoppingToken)
    {
        throw new PaymentProviderUnavailableException();
    }

    public ProviderEvent? ReadEvent(string payload, string? signature)
    {
        return null;
    }
}
