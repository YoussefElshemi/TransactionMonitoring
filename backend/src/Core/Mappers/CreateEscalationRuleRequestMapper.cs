using Core.Enums;
using Core.Models.Escalations;
using Core.ValueObjects;

namespace Core.Mappers;

public static class CreateEscalationRuleRequestMapper
{
    public static EscalationRule ToModel(this CreateEscalationRuleRequest createEscalationRuleRequest, TimeProvider timeProvider, Mode mode)
    {
        var now = timeProvider.GetUtcNow();

        return new EscalationRule
        {
            Mode = mode,
            EscalationRuleId = new EscalationRuleId(Guid.NewGuid()),
            OrganizationId = createEscalationRuleRequest.OrganizationId,
            Name = createEscalationRuleRequest.Name,
            Description = createEscalationRuleRequest.Description,
            Priority = createEscalationRuleRequest.Priority,
            IsEnabled = createEscalationRuleRequest.IsEnabled,
            UnassignedFor = createEscalationRuleRequest.UnassignedFor,
            HoldExpiryWithin = createEscalationRuleRequest.HoldExpiryWithin,
            MinimumSeverity = createEscalationRuleRequest.MinimumSeverity,
            Matcher = createEscalationRuleRequest.Matcher,
            TargetQueueId = createEscalationRuleRequest.TargetQueueId,
            RaiseSeverityTo = createEscalationRuleRequest.RaiseSeverityTo,
            CreatedAt = new CreatedAt(now),
            UpdatedAt = new UpdatedAt(now),
            IsDeleted = new IsDeleted(false),
            DeletedAt = null
        };
    }
}
