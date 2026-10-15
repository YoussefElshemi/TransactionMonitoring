using Core.Enums;
using Core.Models.ApiClients;
using Core.ValueObjects;
using Infrastructure.Entities;

namespace Infrastructure.Mappers;

public static class ApiClientMapper
{
    public static ApiClient ToModel(this ApiClientEntity apiClientEntity)
    {
        return new ApiClient
        {
            Mode = (Mode)apiClientEntity.ModeId,
            ApiVersion = new ApiVersion(apiClientEntity.ApiVersion),
            ApiClientId = new ApiClientId(apiClientEntity.ApiClientId),
            ClientId = new ClientId(apiClientEntity.ClientId),
            FriendlyName = new FriendlyName(apiClientEntity.FriendlyName),
            IsEnabled = new IsEnabled(apiClientEntity.IsEnabled),
            OrganizationId = new OrganizationId(apiClientEntity.OrganizationId),
            Organization = apiClientEntity.Organization.ToModel(),
            CreatedByUserId = new ApplicationUserId(apiClientEntity.CreatedByUserId),
            Scopes = apiClientEntity.Scopes.Select(x => (ApiScope)x.ApiScopeId).ToList(),
            Secrets = apiClientEntity.Secrets.Select(x => x.ToModel()).ToList(),
            CreatedAt = new CreatedAt(apiClientEntity.CreatedAt),
            UpdatedAt = new UpdatedAt(apiClientEntity.UpdatedAt),
            IsDeleted = new IsDeleted(apiClientEntity.IsDeleted),
            DeletedAt = apiClientEntity.DeletedAt.HasValue ? new DeletedAt(apiClientEntity.DeletedAt.Value) : null
        };
    }

    public static ApiClientEntity ToEntity(this ApiClient apiClient)
    {
        return new ApiClientEntity
        {
            ModeId = (int)apiClient.Mode,
            ApiVersion = apiClient.ApiVersion,
            ApiClientId = apiClient.ApiClientId,
            ClientId = apiClient.ClientId,
            FriendlyName = apiClient.FriendlyName,
            IsEnabled = apiClient.IsEnabled,
            OrganizationId = apiClient.OrganizationId,
            CreatedByUserId = apiClient.CreatedByUserId,
            Scopes = apiClient.Scopes
                .Select(x => new ApiClientScopeEntity
                {
                    ApiClientId = apiClient.ApiClientId,
                    ApiScopeId = (int)x,
                    CreatedAt = apiClient.CreatedAt,
                    UpdatedAt = apiClient.UpdatedAt
                })
                .ToList(),
            CreatedAt = apiClient.CreatedAt,
            UpdatedAt = apiClient.UpdatedAt,
            IsDeleted = apiClient.IsDeleted,
            DeletedAt = apiClient.DeletedAt
        };
    }
}
