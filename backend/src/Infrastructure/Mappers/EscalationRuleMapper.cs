using Core.Enums;
using Core.Models.Escalations;
using Core.ValueObjects;
using Infrastructure.Entities;

namespace Infrastructure.Mappers;

public static class EscalationRuleMapper
{
    public static EscalationRule ToModel(this EscalationRuleEntity escalationRuleEntity)
    {
        return new EscalationRule
        {
            Mode = (Mode)escalationRuleEntity.ModeId,
            EscalationRuleId = new EscalationRuleId(escalationRuleEntity.EscalationRuleId),
            OrganizationId = new OrganizationId(escalationRuleEntity.OrganizationId),
            Name = new EscalationRuleName(escalationRuleEntity.Name),
            Description = !string.IsNullOrWhiteSpace(escalationRuleEntity.Description)
                ? new EscalationRuleDescription(escalationRuleEntity.Description)
                : null,
            Priority = new EscalationPriority(escalationRuleEntity.Priority),
            IsEnabled = new IsEnabled(escalationRuleEntity.IsEnabled),
            UnassignedFor = escalationRuleEntity.UnassignedForMinutes.HasValue
                ? new UnassignedFor(escalationRuleEntity.UnassignedForMinutes.Value)
                : null,
            HoldExpiryWithin = escalationRuleEntity.HoldExpiryWithinMinutes.HasValue
                ? new HoldExpiryWithin(escalationRuleEntity.HoldExpiryWithinMinutes.Value)
                : null,
            MinimumSeverity = escalationRuleEntity.MinimumSeverityId.HasValue
                ? (RuleSeverity)escalationRuleEntity.MinimumSeverityId.Value
                : null,
            Matcher = escalationRuleEntity.Matcher.ToRuleBody(),
            TargetQueueId = escalationRuleEntity.TargetQueueId.HasValue
                ? new QueueId(escalationRuleEntity.TargetQueueId.Value)
                : null,
            RaiseSeverityTo = escalationRuleEntity.RaiseSeverityToId.HasValue
                ? (RuleSeverity)escalationRuleEntity.RaiseSeverityToId.Value
                : null,
            CreatedAt = new CreatedAt(escalationRuleEntity.CreatedAt),
            UpdatedAt = new UpdatedAt(escalationRuleEntity.UpdatedAt),
            IsDeleted = new IsDeleted(escalationRuleEntity.IsDeleted),
            DeletedAt = escalationRuleEntity.DeletedAt.HasValue ? new DeletedAt(escalationRuleEntity.DeletedAt.Value) : null
        };
    }

    public static EscalationRuleEntity ToEntity(this EscalationRule escalationRule)
    {
        return new EscalationRuleEntity
        {
            ModeId = (int)escalationRule.Mode,
            EscalationRuleId = escalationRule.EscalationRuleId,
            OrganizationId = escalationRule.OrganizationId,
            Name = escalationRule.Name,
            Description = escalationRule.Description?.ToString(),
            Priority = escalationRule.Priority,
            IsEnabled = escalationRule.IsEnabled,
            UnassignedForMinutes = escalationRule.UnassignedFor.HasValue ? escalationRule.UnassignedFor.Value : null,
            HoldExpiryWithinMinutes = escalationRule.HoldExpiryWithin.HasValue ? escalationRule.HoldExpiryWithin.Value : null,
            MinimumSeverityId = escalationRule.MinimumSeverity.HasValue ? (int)escalationRule.MinimumSeverity.Value : null,
            Matcher = escalationRule.Matcher?.ToBase64String(),
            TargetQueueId = escalationRule.TargetQueueId.HasValue ? escalationRule.TargetQueueId.Value : null,
            RaiseSeverityToId = escalationRule.RaiseSeverityTo.HasValue ? (int)escalationRule.RaiseSeverityTo.Value : null,
            CreatedAt = escalationRule.CreatedAt,
            UpdatedAt = escalationRule.UpdatedAt,
            IsDeleted = escalationRule.IsDeleted,
            DeletedAt = escalationRule.DeletedAt
        };
    }
}
