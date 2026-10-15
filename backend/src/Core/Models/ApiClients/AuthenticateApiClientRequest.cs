using Core.Enums;
using Core.ValueObjects;

namespace Core.Models.ApiClients;

public record AuthenticateApiClientRequest
{
    public required ClientId ClientId { get; init; }
    public required ClientSecret ClientSecret { get; init; }
    public required List<ApiScope>? Scopes { get; init; }
}
