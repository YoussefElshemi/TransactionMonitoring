using Core.Enums;
using Core.Models.ApiClients;
using Core.ValueObjects;
using Infrastructure.Entities;

namespace Infrastructure.Mappers;

public static class ApiClientSecretMapper
{
    public static ApiClientSecret ToModel(this ApiClientSecretEntity apiClientSecretEntity)
    {
        return new ApiClientSecret
        {
            Mode = (Mode)apiClientSecretEntity.ModeId,
            ApiClientSecretId = new ApiClientSecretId(apiClientSecretEntity.ApiClientSecretId),
            ApiClientId = new ApiClientId(apiClientSecretEntity.ApiClientId),
            OrganizationId = new OrganizationId(apiClientSecretEntity.OrganizationId),
            SecretHash = new SecretHash(apiClientSecretEntity.SecretHash),
            SecretPrefix = new SecretPrefix(apiClientSecretEntity.SecretPrefix),
            ExpiresAt = apiClientSecretEntity.ExpiresAt.HasValue ? new ExpiresAt(apiClientSecretEntity.ExpiresAt.Value) : null,
            RevokedAt = apiClientSecretEntity.RevokedAt.HasValue ? new RevokedAt(apiClientSecretEntity.RevokedAt.Value) : null,
            LastUsedAt = apiClientSecretEntity.LastUsedAt.HasValue ? new LastUsedAt(apiClientSecretEntity.LastUsedAt.Value) : null,
            CreatedByUserId = new ApplicationUserId(apiClientSecretEntity.CreatedByUserId),
            CreatedAt = new CreatedAt(apiClientSecretEntity.CreatedAt),
            UpdatedAt = new UpdatedAt(apiClientSecretEntity.UpdatedAt),
            IsDeleted = new IsDeleted(apiClientSecretEntity.IsDeleted),
            DeletedAt = apiClientSecretEntity.DeletedAt.HasValue ? new DeletedAt(apiClientSecretEntity.DeletedAt.Value) : null
        };
    }

    public static ApiClientSecretEntity ToEntity(this ApiClientSecret apiClientSecret)
    {
        return new ApiClientSecretEntity
        {
            ModeId = (int)apiClientSecret.Mode,
            ApiClientSecretId = apiClientSecret.ApiClientSecretId,
            ApiClientId = apiClientSecret.ApiClientId,
            OrganizationId = apiClientSecret.OrganizationId,
            SecretHash = apiClientSecret.SecretHash,
            SecretPrefix = apiClientSecret.SecretPrefix,
            ExpiresAt = apiClientSecret.ExpiresAt,
            RevokedAt = apiClientSecret.RevokedAt,
            LastUsedAt = apiClientSecret.LastUsedAt,
            CreatedByUserId = apiClientSecret.CreatedByUserId,
            CreatedAt = apiClientSecret.CreatedAt,
            UpdatedAt = apiClientSecret.UpdatedAt,
            IsDeleted = apiClientSecret.IsDeleted,
            DeletedAt = apiClientSecret.DeletedAt
        };
    }
}
