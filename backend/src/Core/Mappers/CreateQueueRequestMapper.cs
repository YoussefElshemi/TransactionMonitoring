using Core.Enums;
using Core.Models.Queues;
using Core.ValueObjects;

namespace Core.Mappers;

public static class CreateQueueRequestMapper
{
    public static Queue ToModel(this CreateQueueRequest createQueueRequest, TimeProvider timeProvider, Mode mode)
    {
        return new Queue
        {
            Mode = mode,
            QueueId = new QueueId(Guid.NewGuid()),
            OrganizationId = createQueueRequest.OrganizationId,
            SubOrganizationId = createQueueRequest.SubOrganizationId,
            Name = createQueueRequest.Name,
            Description = createQueueRequest.Description,
            Priority = createQueueRequest.Priority,
            RoutingPolicy = createQueueRequest.RoutingPolicy,
            MinimumSeverity = createQueueRequest.MinimumSeverity,
            RuleIds = createQueueRequest.RuleIds,
            Matcher = createQueueRequest.Matcher,
            IsEnabled = createQueueRequest.IsEnabled,
            Members = [],
            CreatedAt = new CreatedAt(timeProvider.GetUtcNow()),
            UpdatedAt = new UpdatedAt(timeProvider.GetUtcNow()),
            IsDeleted = new IsDeleted(false),
            DeletedAt = null
        };
    }
}
