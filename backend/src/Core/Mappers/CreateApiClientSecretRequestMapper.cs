using Core.Enums;
using Core.Helpers;
using Core.Models.ApiClients;
using Core.ValueObjects;

namespace Core.Mappers;

public static class CreateApiClientSecretRequestMapper
{
    public static ApiClientSecret ToModel(
        this CreateApiClientSecretRequest createApiClientSecretRequest,
        ClientSecret clientSecret,
        TimeProvider timeProvider, Mode mode)
    {
        return new ApiClientSecret
        {
            Mode = mode,
            ApiClientSecretId = new ApiClientSecretId(Guid.NewGuid()),
            ApiClientId = createApiClientSecretRequest.ApiClientId,
            OrganizationId = createApiClientSecretRequest.OrganizationId,
            SecretHash = ApiClientCredentialHelper.Hash(clientSecret),
            SecretPrefix = ApiClientCredentialHelper.ToSecretPrefix(clientSecret),
            ExpiresAt = createApiClientSecretRequest.ExpiresAt,
            RevokedAt = null,
            LastUsedAt = null,
            CreatedByUserId = createApiClientSecretRequest.UserId,
            CreatedAt = new CreatedAt(timeProvider.GetUtcNow()),
            UpdatedAt = new UpdatedAt(timeProvider.GetUtcNow()),
            IsDeleted = new IsDeleted(false),
            DeletedAt = null
        };
    }
}
