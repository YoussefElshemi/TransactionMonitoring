using Core.Enums;
using Core.ValueObjects;

namespace Core.Models.ApiClients;

public record UpdateApiClientRequest
{
    public required ApiClientId ApiClientId { get; init; }
    public required FriendlyName? FriendlyName { get; init; }
    public required IsEnabled? IsEnabled { get; init; }
    public required List<ApiScope>? Scopes { get; init; }
    public required ApiVersion? ApiVersion { get; init; }
}
