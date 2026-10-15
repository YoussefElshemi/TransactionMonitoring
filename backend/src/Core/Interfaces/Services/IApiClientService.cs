using Core.Models.ApiClients;
using Core.ValueObjects;

namespace Core.Interfaces.Services;

public interface IApiClientService : IPagedService<ListApiClientRequest, ApiClient>
{
    Task<CreatedApiClient> CreateAsync(CreateApiClientRequest createApiClientRequest, CancellationToken stoppingToken);
    Task UpdateAsync(UpdateApiClientRequest updateApiClientRequest, CancellationToken stoppingToken);
    Task<Exists> ExistsAsync(OrganizationId organizationId, ApiClientId apiClientId, IncludeDeleted includeDeleted, CancellationToken stoppingToken);
    Task<Exists> IsActiveAsync(ApiClientId apiClientId, CancellationToken stoppingToken);
    Task<ApiClient> GetAsync(ApiClientId apiClientId, CancellationToken stoppingToken);
    Task<List<ApiClient>> ListByIdsAsync(IReadOnlyCollection<ApiClientId> apiClientIds, CancellationToken stoppingToken);
    Task DeleteAsync(ApiClientId apiClientId, CancellationToken stoppingToken);
    Task RecoverAsync(ApiClientId apiClientId, CancellationToken stoppingToken);

    Task<CreatedApiClientSecret> CreateSecretAsync(CreateApiClientSecretRequest createApiClientSecretRequest, CancellationToken stoppingToken);
    Task<List<ApiClientSecret>> ListSecretsAsync(ApiClientId apiClientId, CancellationToken stoppingToken);
    Task<Exists> SecretExistsAsync(ApiClientId apiClientId, ApiClientSecretId apiClientSecretId, CancellationToken stoppingToken);
    Task<Exists> IsSecretActiveAsync(ApiClientSecretId apiClientSecretId, CancellationToken stoppingToken);
    Task RevokeSecretAsync(ApiClientSecretId apiClientSecretId, CancellationToken stoppingToken);

    Task<ApiClientAuthenticationResult> AuthenticateAsync(AuthenticateApiClientRequest authenticateApiClientRequest, CancellationToken stoppingToken);
}
