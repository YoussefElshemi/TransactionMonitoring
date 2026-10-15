using Core.Enums;
using Core.Models.Queues;
using Core.ValueObjects;
using Presentation.Mappers.Rules;
using Presentation.Models.Queues;

namespace Presentation.Mappers.Queues;

public static class CreateQueueRequestMapper
{
    public static CreateQueueRequest ToModel(this CreateQueueRequestDto createQueueRequestDto)
    {
        return new CreateQueueRequest
        {
            OrganizationId = new OrganizationId(createQueueRequestDto.OrganizationId),
            Name = new QueueName(createQueueRequestDto.Body.Name),
            Description = !string.IsNullOrWhiteSpace(createQueueRequestDto.Body.Description)
                ? new QueueDescription(createQueueRequestDto.Body.Description)
                : null,
            Priority = new QueuePriority(createQueueRequestDto.Body.Priority ?? QueuePriority.MaxValue),
            RoutingPolicy = Enum.Parse<QueueRoutingPolicy>(createQueueRequestDto.Body.RoutingPolicy, true),
            MinimumSeverity = !string.IsNullOrWhiteSpace(createQueueRequestDto.Body.MinimumSeverity)
                ? Enum.Parse<RuleSeverity>(createQueueRequestDto.Body.MinimumSeverity, true)
                : null,
            RuleIds = (createQueueRequestDto.Body.RuleIds ?? []).Select(x => new RuleId(x)).ToList(),
            Matcher = createQueueRequestDto.Body.Matcher?.ToModel(),
            IsEnabled = new IsEnabled(createQueueRequestDto.Body.IsEnabled),
            MemberUserIds = (createQueueRequestDto.Body.MemberUserIds ?? []).Select(x => new ApplicationUserId(x)).ToList(),
            SubOrganizationId = createQueueRequestDto.Body.SubOrganizationId.HasValue
                ? new SubOrganizationId(createQueueRequestDto.Body.SubOrganizationId.Value)
                : null
        };
    }
}
