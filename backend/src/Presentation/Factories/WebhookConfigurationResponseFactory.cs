using Core.Models.WebhookConfigurations;
using Core.ValueObjects;
using Presentation.Mappers.OrganizationWebhookConfigurations;
using Presentation.Models.WebhookConfigurations;

namespace Presentation.Factories;

public class WebhookConfigurationResponseFactory(RelatedRecordLoader relatedRecordLoader)
{
    public async Task<WebhookConfigurationResourceDto> CreateAsync(
        WebhookConfiguration webhookConfiguration, EnrichmentContext context, CancellationToken stoppingToken)
    {
        var resources = await CreateAsync([webhookConfiguration], context, stoppingToken);

        return resources[0];
    }

    public async Task<WebhookConfigurationResourceDto[]> CreateAsync(
        IReadOnlyCollection<WebhookConfiguration> webhookConfigurations, EnrichmentContext context, CancellationToken stoppingToken)
    {
        if (webhookConfigurations.Count == 0)
        {
            return [];
        }

        var organizations = await relatedRecordLoader.LoadOrganizationsAsync(
            webhookConfigurations.Select(x => (OrganizationId?)x.OrganizationId), context, stoppingToken);

        return webhookConfigurations.Select(x => new WebhookConfigurationResourceDto
        {
            WebhookConfiguration = x.ToDto(),
            Organization = organizations.GetValueOrDefault(x.OrganizationId)
        }).ToArray();
    }
}
