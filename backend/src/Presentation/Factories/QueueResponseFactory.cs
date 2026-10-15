using Core.Models.Queues;
using Core.ValueObjects;
using Presentation.Mappers.Queues;
using Presentation.Models.Queues;

namespace Presentation.Factories;

public class QueueResponseFactory(RelatedRecordLoader relatedRecordLoader)
{
    public async Task<QueueResourceDto> CreateAsync(Queue queue, EnrichmentContext context, CancellationToken stoppingToken)
    {
        var resources = await CreateAsync([queue], context, stoppingToken);

        return resources[0];
    }

    public async Task<QueueResourceDto[]> CreateAsync(
        IReadOnlyCollection<Queue> queues, EnrichmentContext context, CancellationToken stoppingToken)
    {
        if (queues.Count == 0)
        {
            return [];
        }

        var organizations = await relatedRecordLoader.LoadOrganizationsAsync(
            queues.Select(x => (OrganizationId?)x.OrganizationId), context, stoppingToken);

        return queues.Select(x => new QueueResourceDto
        {
            Queue = x.ToDto(),
            Organization = organizations.GetValueOrDefault(x.OrganizationId)
        }).ToArray();
    }
}
