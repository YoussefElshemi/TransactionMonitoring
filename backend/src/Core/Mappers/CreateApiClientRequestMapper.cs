using Core.Constants;
using Core.Enums;
using Core.Helpers;
using Core.Models.ApiClients;
using Core.ValueObjects;

namespace Core.Mappers;

public static class CreateApiClientRequestMapper
{
    public static ApiClient ToModel(this CreateApiClientRequest createApiClientRequest, TimeProvider timeProvider, Mode mode)
    {
        return new ApiClient
        {
            Mode = mode,
            ApiVersion = ApiVersions.Current,
            ApiClientId = new ApiClientId(Guid.NewGuid()),
            ClientId = ApiClientCredentialHelper.GenerateClientId(mode),
            FriendlyName = createApiClientRequest.FriendlyName,
            IsEnabled = new IsEnabled(true),
            OrganizationId = createApiClientRequest.OrganizationId,
            Organization = null,
            CreatedByUserId = createApiClientRequest.UserId,
            Scopes = createApiClientRequest.Scopes,
            Secrets = [],
            CreatedAt = new CreatedAt(timeProvider.GetUtcNow()),
            UpdatedAt = new UpdatedAt(timeProvider.GetUtcNow()),
            IsDeleted = new IsDeleted(false),
            DeletedAt = null
        };
    }
}
