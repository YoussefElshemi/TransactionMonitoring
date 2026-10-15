using Core.Enums;
using Core.Extensions;
using Core.Models.Queues;
using Core.Models.Rules;
using Core.ValueObjects;
using Infrastructure.Entities;

namespace Infrastructure.Mappers;

public static class QueueMapper
{
    public static Queue ToModel(this QueueEntity queueEntity)
    {
        return new Queue
        {
            Mode = (Mode)queueEntity.ModeId,
            QueueId = new QueueId(queueEntity.QueueId),
            OrganizationId = new OrganizationId(queueEntity.OrganizationId),
            SubOrganizationId = queueEntity.SubOrganizationId.HasValue ? new SubOrganizationId(queueEntity.SubOrganizationId.Value) : null,
            Name = new QueueName(queueEntity.Name),
            Description = !string.IsNullOrWhiteSpace(queueEntity.Description) ? new QueueDescription(queueEntity.Description) : null,
            Priority = new QueuePriority(queueEntity.Priority),
            RoutingPolicy = (QueueRoutingPolicy)queueEntity.RoutingPolicyId,
            MinimumSeverity = queueEntity.MinimumSeverityId.HasValue ? (RuleSeverity)queueEntity.MinimumSeverityId.Value : null,
            RuleIds = queueEntity.QueueRules.Select(x => new RuleId(x.RuleId)).ToList(),
            Matcher = queueEntity.Matcher.ToRuleBody(),
            IsEnabled = new IsEnabled(queueEntity.IsEnabled),
            Members = queueEntity.Members.Select(x => x.ToModel()).ToList(),
            CreatedAt = new CreatedAt(queueEntity.CreatedAt),
            UpdatedAt = new UpdatedAt(queueEntity.UpdatedAt),
            IsDeleted = new IsDeleted(queueEntity.IsDeleted),
            DeletedAt = queueEntity.DeletedAt.HasValue ? new DeletedAt(queueEntity.DeletedAt.Value) : null
        };
    }

    public static QueueEntity ToEntity(this Queue queue)
    {
        return new QueueEntity
        {
            ModeId = (int)queue.Mode,
            QueueId = queue.QueueId,
            OrganizationId = queue.OrganizationId,
            SubOrganizationId = queue.SubOrganizationId.HasValue ? (Guid)queue.SubOrganizationId.Value : null,
            Name = queue.Name,
            Description = queue.Description?.ToString(),
            Priority = queue.Priority,
            RoutingPolicyId = (int)queue.RoutingPolicy,
            MinimumSeverityId = queue.MinimumSeverity.HasValue ? (int)queue.MinimumSeverity.Value : null,
            Matcher = queue.Matcher?.ToBase64String(),
            IsEnabled = queue.IsEnabled,
            CreatedAt = queue.CreatedAt,
            UpdatedAt = queue.UpdatedAt,
            IsDeleted = queue.IsDeleted,
            DeletedAt = queue.DeletedAt
        };
    }

    public static RuleBody? ToRuleBody(this string? matcher)
    {
        return string.IsNullOrWhiteSpace(matcher) ? null : matcher.FromBase64String().TryDeserialize<RuleBodyModel>().ToModel();
    }
}
