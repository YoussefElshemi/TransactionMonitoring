using Core.Models.OutboxMessages;
using Presentation.Mappers.Webhooks;
using Presentation.Models.Webhooks;

namespace Presentation.Factories;

public class WebhookDeliveryResponseFactory(RelatedRecordLoader relatedRecordLoader)
{
    public async Task<WebhookDeliveryResourceDto> CreateAsync(
        OutboxMessage outboxMessage, EnrichmentContext context, CancellationToken stoppingToken)
    {
        var resources = await CreateAsync([outboxMessage], context, stoppingToken);

        return resources[0];
    }

    public async Task<WebhookDeliveryResourceDto[]> CreateAsync(
        IReadOnlyCollection<OutboxMessage> outboxMessages, EnrichmentContext context, CancellationToken stoppingToken)
    {
        if (outboxMessages.Count == 0)
        {
            return [];
        }

        var organizations = await relatedRecordLoader.LoadOrganizationsAsync(
            outboxMessages.Select(x => x.OrganizationId), context, stoppingToken);

        return outboxMessages.Select(x => new WebhookDeliveryResourceDto
        {
            WebhookDelivery = x.ToDeliveryDto(),
            Organization = x.OrganizationId.HasValue ? organizations.GetValueOrDefault(x.OrganizationId.Value) : null
        }).ToArray();
    }
}
