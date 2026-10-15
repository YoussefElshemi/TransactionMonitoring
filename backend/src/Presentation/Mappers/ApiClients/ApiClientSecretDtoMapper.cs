using Core.Models.ApiClients;
using Presentation.Models.ApiClients;

namespace Presentation.Mappers.ApiClients;

public static class ApiClientSecretDtoMapper
{
    public static ApiClientSecretResponseDto ToDto(this ApiClientSecret apiClientSecret)
    {
        return new ApiClientSecretResponseDto
        {
            Mode = apiClientSecret.Mode.ToString(),
            ApiClientSecretId = apiClientSecret.ApiClientSecretId,
            ApiClientId = apiClientSecret.ApiClientId,
            SecretPrefix = apiClientSecret.SecretPrefix,
            ExpiresAt = apiClientSecret.ExpiresAt,
            RevokedAt = apiClientSecret.RevokedAt,
            LastUsedAt = apiClientSecret.LastUsedAt,
            CreatedAt = apiClientSecret.CreatedAt,
            UpdatedAt = apiClientSecret.UpdatedAt
        };
    }

    public static CreatedApiClientSecretResponseDto ToSecretDto(this CreatedApiClientSecret createdApiClientSecret)
    {
        var apiClientSecret = createdApiClientSecret.ApiClientSecret;

        return new CreatedApiClientSecretResponseDto
        {
            Mode = apiClientSecret.Mode.ToString(),
            ApiClientSecretId = apiClientSecret.ApiClientSecretId,
            ApiClientId = apiClientSecret.ApiClientId,
            ClientSecret = createdApiClientSecret.ClientSecret,
            SecretPrefix = apiClientSecret.SecretPrefix,
            ExpiresAt = apiClientSecret.ExpiresAt,
            RevokedAt = apiClientSecret.RevokedAt,
            LastUsedAt = apiClientSecret.LastUsedAt,
            CreatedAt = apiClientSecret.CreatedAt,
            UpdatedAt = apiClientSecret.UpdatedAt
        };
    }
}
