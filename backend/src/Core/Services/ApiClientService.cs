using Core.Enums;
using Core.Helpers;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Mappers;
using Core.Models.ApiClients;
using Core.Models.Common;
using Core.ValueObjects;

namespace Core.Services;

public class ApiClientService(
    IApiClientRepository apiClientRepository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IModeContext modeContext) : IApiClientService
{
    public async Task<CreatedApiClient> CreateAsync(CreateApiClientRequest createApiClientRequest, CancellationToken stoppingToken)
    {
        var apiClient = createApiClientRequest.ToModel(timeProvider, modeContext.CurrentOrLive);
        var clientSecret = ApiClientCredentialHelper.GenerateClientSecret(modeContext.CurrentOrLive);

        var createApiClientSecretRequest = new CreateApiClientSecretRequest
        {
            ApiClientId = apiClient.ApiClientId,
            OrganizationId = createApiClientRequest.OrganizationId,
            UserId = createApiClientRequest.UserId,
            ExpiresAt = null
        };

        await apiClientRepository.AddAsync(apiClient, stoppingToken);
        await apiClientRepository.AddSecretAsync(createApiClientSecretRequest.ToModel(clientSecret, timeProvider, modeContext.CurrentOrLive), stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);

        return new CreatedApiClient
        {
            ApiClient = await GetAsync(apiClient.ApiClientId, stoppingToken),
            ClientSecret = clientSecret
        };
    }

    public async Task UpdateAsync(UpdateApiClientRequest updateApiClientRequest, CancellationToken stoppingToken)
    {
        await apiClientRepository.UpdateAsync(updateApiClientRequest, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);
    }

    public Task<Exists> ExistsAsync(OrganizationId organizationId, ApiClientId apiClientId, IncludeDeleted includeDeleted, CancellationToken stoppingToken)
    {
        return apiClientRepository.ExistsAsync(organizationId, apiClientId, includeDeleted, stoppingToken);
    }

    public Task<Exists> IsActiveAsync(ApiClientId apiClientId, CancellationToken stoppingToken)
    {
        return apiClientRepository.IsActiveAsync(apiClientId, stoppingToken);
    }

    public Task<PagedResponse<ApiClient>> ListAsync(ListApiClientRequest listApiClientRequest, CancellationToken stoppingToken)
    {
        return apiClientRepository.ListAsync(listApiClientRequest, stoppingToken);
    }

    public Task<ApiClient> GetAsync(ApiClientId apiClientId, CancellationToken stoppingToken)
    {
        return apiClientRepository.GetAsync(apiClientId, stoppingToken);
    }

    public Task<List<ApiClient>> ListByIdsAsync(IReadOnlyCollection<ApiClientId> apiClientIds, CancellationToken stoppingToken)
    {
        return apiClientRepository.ListByIdsAsync(apiClientIds, stoppingToken);
    }

    public async Task DeleteAsync(ApiClientId apiClientId, CancellationToken stoppingToken)
    {
        await apiClientRepository.DeleteAsync(apiClientId, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);
    }

    public async Task RecoverAsync(ApiClientId apiClientId, CancellationToken stoppingToken)
    {
        await apiClientRepository.RecoverAsync(apiClientId, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);
    }

    public Task<TotalCount> GetCountAsync(ListApiClientRequest listApiClientRequest, CancellationToken stoppingToken)
    {
        return apiClientRepository.GetCountAsync(listApiClientRequest, stoppingToken);
    }

    public async Task<CreatedApiClientSecret> CreateSecretAsync(CreateApiClientSecretRequest createApiClientSecretRequest, CancellationToken stoppingToken)
    {
        var clientSecret = ApiClientCredentialHelper.GenerateClientSecret(modeContext.CurrentOrLive);
        var apiClientSecret = createApiClientSecretRequest.ToModel(clientSecret, timeProvider, modeContext.CurrentOrLive);

        await apiClientRepository.AddSecretAsync(apiClientSecret, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);

        return new CreatedApiClientSecret
        {
            ApiClientSecret = apiClientSecret,
            ClientSecret = clientSecret
        };
    }

    public Task<List<ApiClientSecret>> ListSecretsAsync(ApiClientId apiClientId, CancellationToken stoppingToken)
    {
        return apiClientRepository.ListSecretsAsync(apiClientId, stoppingToken);
    }

    public Task<Exists> SecretExistsAsync(ApiClientId apiClientId, ApiClientSecretId apiClientSecretId, CancellationToken stoppingToken)
    {
        return apiClientRepository.SecretExistsAsync(apiClientId, apiClientSecretId, stoppingToken);
    }

    public Task<Exists> IsSecretActiveAsync(ApiClientSecretId apiClientSecretId, CancellationToken stoppingToken)
    {
        return apiClientRepository.IsSecretActiveAsync(apiClientSecretId, stoppingToken);
    }

    public async Task RevokeSecretAsync(ApiClientSecretId apiClientSecretId, CancellationToken stoppingToken)
    {
        await apiClientRepository.RevokeSecretAsync(apiClientSecretId, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);
    }

    public async Task<ApiClientAuthenticationResult> AuthenticateAsync(
        AuthenticateApiClientRequest authenticateApiClientRequest,
        CancellationToken stoppingToken)
    {
        var apiClient = await apiClientRepository.FindActiveByClientIdAsync(authenticateApiClientRequest.ClientId, stoppingToken);

        if (apiClient is null)
        {
            ApiClientCredentialHelper.Hash(authenticateApiClientRequest.ClientSecret);

            return InvalidClient();
        }

        var now = timeProvider.GetUtcNow();

        var matchedSecret = apiClient.Secrets.FirstOrDefault(x =>
            ApiClientCredentialHelper.Verify(authenticateApiClientRequest.ClientSecret, x.SecretHash) &&
            x.RevokedAt is null &&
            (x.ExpiresAt is not { } expiresAt || now < expiresAt));

        if (matchedSecret is null)
        {
            return InvalidClient();
        }

        var scopes = authenticateApiClientRequest.Scopes ?? apiClient.Scopes;

        if (scopes.Except(apiClient.Scopes).Any())
        {
            return new ApiClientAuthenticationResult
            {
                Outcome = ApiClientAuthenticationOutcome.InvalidScope,
                AuthenticatedApiClient = null
            };
        }

        await apiClientRepository.StampSecretUsedAsync(matchedSecret.ApiClientSecretId, stoppingToken);

        return new ApiClientAuthenticationResult
        {
            Outcome = ApiClientAuthenticationOutcome.Success,
            AuthenticatedApiClient = new AuthenticatedApiClient
            {
                ApiClientId = apiClient.ApiClientId,
                ApiClientSecretId = matchedSecret.ApiClientSecretId,
                ClientId = apiClient.ClientId,
                OrganizationId = apiClient.OrganizationId,
                Scopes = scopes.Distinct().ToList(),
                Mode = apiClient.Mode,
                ApiVersion = apiClient.ApiVersion
            }
        };
    }

    private static ApiClientAuthenticationResult InvalidClient()
    {
        return new ApiClientAuthenticationResult
        {
            Outcome = ApiClientAuthenticationOutcome.InvalidClient,
            AuthenticatedApiClient = null
        };
    }
}
