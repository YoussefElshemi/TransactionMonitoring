using Core.Models.Escalations;
using Presentation.Mappers.Rules;
using Presentation.Models.Escalations;

namespace Presentation.Mappers.Escalations;

public static class EscalationRuleDtoMapper
{
    public static EscalationRuleResponseDto ToDto(this EscalationRule escalationRule)
    {
        return new EscalationRuleResponseDto
        {
            Mode = escalationRule.Mode.ToString(),
            EscalationRuleId = escalationRule.EscalationRuleId,
            OrganizationId = escalationRule.OrganizationId,
            Name = escalationRule.Name,
            Description = escalationRule.Description?.ToString(),
            Priority = escalationRule.Priority,
            IsEnabled = escalationRule.IsEnabled,
            UnassignedForMinutes = escalationRule.UnassignedFor.HasValue ? escalationRule.UnassignedFor.Value : null,
            HoldExpiryWithinMinutes = escalationRule.HoldExpiryWithin.HasValue ? escalationRule.HoldExpiryWithin.Value : null,
            MinimumSeverity = escalationRule.MinimumSeverity?.ToString(),
            Matcher = escalationRule.Matcher?.ToDto(),
            TargetQueueId = escalationRule.TargetQueueId.HasValue ? escalationRule.TargetQueueId.Value : null,
            RaiseSeverityTo = escalationRule.RaiseSeverityTo?.ToString(),
            CreatedAt = escalationRule.CreatedAt,
            UpdatedAt = escalationRule.UpdatedAt,
            IsDeleted = escalationRule.IsDeleted,
            DeletedAt = escalationRule.DeletedAt.HasValue ? (DateTimeOffset)escalationRule.DeletedAt.Value : null
        };
    }
}
