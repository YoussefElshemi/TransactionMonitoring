using Core.Enums;
using Core.Models.Queues;
using Core.ValueObjects;
using Presentation.Mappers.Rules;
using Presentation.Models.Queues;

namespace Presentation.Mappers.Queues;

public static class UpdateQueueRequestMapper
{
    public static UpdateQueueRequest ToModel(this UpdateQueueRequestDto updateQueueRequestDto)
    {
        return new UpdateQueueRequest
        {
            QueueId = new QueueId(updateQueueRequestDto.QueueId),
            Name = !string.IsNullOrWhiteSpace(updateQueueRequestDto.Body.Name)
                ? new QueueName(updateQueueRequestDto.Body.Name)
                : null,
            Description = !string.IsNullOrWhiteSpace(updateQueueRequestDto.Body.Description)
                ? new QueueDescription(updateQueueRequestDto.Body.Description)
                : null,
            Priority = updateQueueRequestDto.Body.Priority.HasValue
                ? new QueuePriority(updateQueueRequestDto.Body.Priority.Value)
                : null,
            RoutingPolicy = !string.IsNullOrWhiteSpace(updateQueueRequestDto.Body.RoutingPolicy)
                ? Enum.Parse<QueueRoutingPolicy>(updateQueueRequestDto.Body.RoutingPolicy, true)
                : null,
            MinimumSeverity = !string.IsNullOrWhiteSpace(updateQueueRequestDto.Body.MinimumSeverity)
                ? Enum.Parse<RuleSeverity>(updateQueueRequestDto.Body.MinimumSeverity, true)
                : null,
            ClearMinimumSeverity = new ClearCriteria(updateQueueRequestDto.Body.ClearMinimumSeverity ?? false),
            RuleIds = updateQueueRequestDto.Body.RuleIds?.Select(x => new RuleId(x)).ToList(),
            ClearRuleIds = new ClearCriteria(updateQueueRequestDto.Body.ClearRuleIds ?? false),
            Matcher = updateQueueRequestDto.Body.Matcher?.ToModel(),
            ClearMatcher = new ClearCriteria(updateQueueRequestDto.Body.ClearMatcher ?? false),
            IsEnabled = updateQueueRequestDto.Body.IsEnabled.HasValue
                ? new IsEnabled(updateQueueRequestDto.Body.IsEnabled.Value)
                : null,
            SubOrganizationId = updateQueueRequestDto.Body.SubOrganizationId.HasValue
                ? new SubOrganizationId(updateQueueRequestDto.Body.SubOrganizationId.Value)
                : null,
            ClearSubOrganization = new ClearCriteria(updateQueueRequestDto.Body.ClearSubOrganization ?? false)
        };
    }
}
