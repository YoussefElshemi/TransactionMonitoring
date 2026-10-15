using Core.Enums;

namespace Core.Models.ApiClients;

public record ApiClientAuthenticationResult
{
    public required ApiClientAuthenticationOutcome Outcome { get; init; }
    public required AuthenticatedApiClient? AuthenticatedApiClient { get; init; }
}
