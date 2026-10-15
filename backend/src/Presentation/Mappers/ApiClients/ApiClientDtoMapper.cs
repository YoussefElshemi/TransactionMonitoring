using Core.Helpers;
using Core.Models.ApiClients;
using Presentation.Models.ApiClients;

namespace Presentation.Mappers.ApiClients;

public static class ApiClientDtoMapper
{
    public static ApiClientResponseDto ToDto(this ApiClient apiClient)
    {
        return new ApiClientResponseDto
        {
            Mode = apiClient.Mode.ToString(),
            ApiVersion = apiClient.ApiVersion,
            ApiClientId = apiClient.ApiClientId,
            ClientId = apiClient.ClientId,
            FriendlyName = apiClient.FriendlyName,
            IsEnabled = apiClient.IsEnabled,
            Scopes = apiClient.Scopes.Select(ApiScopeHelper.ToScopeString).ToArray(),
            OrganizationId = apiClient.OrganizationId,
            CreatedByUserId = apiClient.CreatedByUserId,
            CreatedAt = apiClient.CreatedAt,
            UpdatedAt = apiClient.UpdatedAt
        };
    }

    public static CreatedApiClientResponseDto ToSecretDto(this CreatedApiClient createdApiClient)
    {
        var apiClient = createdApiClient.ApiClient;

        return new CreatedApiClientResponseDto
        {
            Mode = apiClient.Mode.ToString(),
            ApiVersion = apiClient.ApiVersion,
            ApiClientId = apiClient.ApiClientId,
            ClientId = apiClient.ClientId,
            ClientSecret = createdApiClient.ClientSecret,
            FriendlyName = apiClient.FriendlyName,
            IsEnabled = apiClient.IsEnabled,
            Scopes = apiClient.Scopes.Select(ApiScopeHelper.ToScopeString).ToArray(),
            OrganizationId = apiClient.OrganizationId,
            CreatedByUserId = apiClient.CreatedByUserId,
            CreatedAt = apiClient.CreatedAt,
            UpdatedAt = apiClient.UpdatedAt
        };
    }
}
