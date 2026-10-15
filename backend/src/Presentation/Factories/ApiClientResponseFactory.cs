using Core.Models.ApiClients;
using Core.ValueObjects;
using Presentation.Mappers.ApiClients;
using Presentation.Models.ApiClients;

namespace Presentation.Factories;

public class ApiClientResponseFactory(RelatedRecordLoader relatedRecordLoader)
{
    public async Task<ApiClientResourceDto> CreateAsync(
        ApiClient apiClient, EnrichmentContext context, CancellationToken stoppingToken)
    {
        var resources = await CreateAsync([apiClient], context, stoppingToken);

        return resources[0];
    }

    public async Task<ApiClientResourceDto[]> CreateAsync(
        IReadOnlyCollection<ApiClient> apiClients, EnrichmentContext context, CancellationToken stoppingToken)
    {
        if (apiClients.Count == 0)
        {
            return [];
        }

        var organizations = await relatedRecordLoader.LoadOrganizationsAsync(
            apiClients.Select(x => (OrganizationId?)x.OrganizationId), context, stoppingToken);
        var createdByUsers = await relatedRecordLoader.LoadUsersAsync(
            apiClients.Select(x => (ApplicationUserId?)x.CreatedByUserId), context, stoppingToken);

        return apiClients.Select(x => new ApiClientResourceDto
        {
            ApiClient = x.ToDto(),
            Organization = organizations.GetValueOrDefault(x.OrganizationId),
            CreatedByUser = createdByUsers.GetValueOrDefault(x.CreatedByUserId)
        }).ToArray();
    }

    public async Task<CreatedApiClientResourceDto> CreateAsync(
        CreatedApiClient createdApiClient, EnrichmentContext context, CancellationToken stoppingToken)
    {
        var apiClient = createdApiClient.ApiClient;

        var organizations = await relatedRecordLoader.LoadOrganizationsAsync(
            [apiClient.OrganizationId], context, stoppingToken);
        var createdByUsers = await relatedRecordLoader.LoadUsersAsync(
            [apiClient.CreatedByUserId], context, stoppingToken);

        return new CreatedApiClientResourceDto
        {
            ApiClient = createdApiClient.ToSecretDto(),
            Organization = organizations.GetValueOrDefault(apiClient.OrganizationId),
            CreatedByUser = createdByUsers.GetValueOrDefault(apiClient.CreatedByUserId)
        };
    }
}
