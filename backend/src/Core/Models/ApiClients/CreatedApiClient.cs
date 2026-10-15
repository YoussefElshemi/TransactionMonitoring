using Core.ValueObjects;

namespace Core.Models.ApiClients;

public record CreatedApiClient
{
    public required ApiClient ApiClient { get; init; }
    public required ClientSecret ClientSecret { get; init; }
}
