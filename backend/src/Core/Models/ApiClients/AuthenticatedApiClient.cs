using Core.Enums;
using Core.ValueObjects;

namespace Core.Models.ApiClients;

public record AuthenticatedApiClient
{
    public required ApiClientId ApiClientId { get; init; }
    public required ApiClientSecretId ApiClientSecretId { get; init; }
    public required ClientId ClientId { get; init; }
    public required OrganizationId OrganizationId { get; init; }
    public required List<ApiScope> Scopes { get; init; }

    public required Mode Mode { get; init; }
    public required ApiVersion ApiVersion { get; init; }
}
