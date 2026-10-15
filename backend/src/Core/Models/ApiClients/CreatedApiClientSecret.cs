using Core.ValueObjects;

namespace Core.Models.ApiClients;

public record CreatedApiClientSecret
{
    public required ApiClientSecret ApiClientSecret { get; init; }
    public required ClientSecret ClientSecret { get; init; }
}
