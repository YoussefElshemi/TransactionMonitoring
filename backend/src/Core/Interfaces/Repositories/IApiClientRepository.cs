using Core.Models.ApiClients;
using Core.ValueObjects;

namespace Core.Interfaces.Repositories;

public interface IApiClientRepository : IPagedRepository<ListApiClientRequest, ApiClient>
{
    Task AddAsync(ApiClient apiClient, CancellationToken stoppingToken);
    Task UpdateAsync(UpdateApiClientRequest updateApiClientRequest, CancellationToken stoppingToken);
    Task<Exists> ExistsAsync(OrganizationId organizationId, ApiClientId apiClientId, IncludeDeleted includeDeleted, CancellationToken stoppingToken);
    Task<Exists> IsActiveAsync(ApiClientId apiClientId, CancellationToken stoppingToken);
    Task<ApiClient> GetAsync(ApiClientId apiClientId, CancellationToken stoppingToken);
    Task<List<ApiClient>> ListByIdsAsync(IReadOnlyCollection<ApiClientId> apiClientIds, CancellationToken stoppingToken);
    Task<ApiClient?> FindActiveByClientIdAsync(ClientId clientId, CancellationToken stoppingToken);
    Task DeleteAsync(ApiClientId apiClientId, CancellationToken stoppingToken);
    Task RecoverAsync(ApiClientId apiClientId, CancellationToken stoppingToken);

    Task AddSecretAsync(ApiClientSecret apiClientSecret, CancellationToken stoppingToken);
    Task<List<ApiClientSecret>> ListSecretsAsync(ApiClientId apiClientId, CancellationToken stoppingToken);
    Task<Exists> SecretExistsAsync(ApiClientId apiClientId, ApiClientSecretId apiClientSecretId, CancellationToken stoppingToken);
    Task<Exists> IsSecretActiveAsync(ApiClientSecretId apiClientSecretId, CancellationToken stoppingToken);
    Task RevokeSecretAsync(ApiClientSecretId apiClientSecretId, CancellationToken stoppingToken);
    Task StampSecretUsedAsync(ApiClientSecretId apiClientSecretId, CancellationToken stoppingToken);
}
