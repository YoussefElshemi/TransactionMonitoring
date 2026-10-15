using Core.ValueObjects;

namespace Core.Models.Providers;

public record ProviderSessionRequest
{
    public required ProviderCustomerReference CustomerReference { get; init; }

    public required HostedSessionUrl ReturnUrl { get; init; }
    public required HostedSessionUrl CancelUrl { get; init; }
}
