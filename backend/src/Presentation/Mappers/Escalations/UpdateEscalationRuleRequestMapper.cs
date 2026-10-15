using Core.Enums;
using Core.Models.Escalations;
using Core.ValueObjects;
using Presentation.Mappers.Rules;
using Presentation.Models.Escalations;

namespace Presentation.Mappers.Escalations;

public static class UpdateEscalationRuleRequestMapper
{
    public static UpdateEscalationRuleRequest ToModel(this UpdateEscalationRuleRequestDto updateEscalationRuleRequestDto)
    {
        var body = updateEscalationRuleRequestDto.Body;

        return new UpdateEscalationRuleRequest
        {
            EscalationRuleId = new EscalationRuleId(updateEscalationRuleRequestDto.EscalationRuleId),
            Name = !string.IsNullOrWhiteSpace(body.Name) ? new EscalationRuleName(body.Name) : null,
            Description = !string.IsNullOrWhiteSpace(body.Description) ? new EscalationRuleDescription(body.Description) : null,
            Priority = body.Priority.HasValue ? new EscalationPriority(body.Priority.Value) : null,
            IsEnabled = body.IsEnabled.HasValue ? new IsEnabled(body.IsEnabled.Value) : null,
            UnassignedFor = body.UnassignedForMinutes.HasValue ? new UnassignedFor(body.UnassignedForMinutes.Value) : null,
            ClearUnassignedFor = new ClearCriteria(body.ClearUnassignedFor ?? false),
            HoldExpiryWithin = body.HoldExpiryWithinMinutes.HasValue ? new HoldExpiryWithin(body.HoldExpiryWithinMinutes.Value) : null,
            ClearHoldExpiryWithin = new ClearCriteria(body.ClearHoldExpiryWithin ?? false),
            MinimumSeverity = !string.IsNullOrWhiteSpace(body.MinimumSeverity)
                ? Enum.Parse<RuleSeverity>(body.MinimumSeverity, true)
                : null,
            ClearMinimumSeverity = new ClearCriteria(body.ClearMinimumSeverity ?? false),
            Matcher = body.Matcher?.ToModel(),
            ClearMatcher = new ClearCriteria(body.ClearMatcher ?? false),
            TargetQueueId = body.TargetQueueId.HasValue ? new QueueId(body.TargetQueueId.Value) : null,
            ClearTargetQueueId = new ClearCriteria(body.ClearTargetQueueId ?? false),
            RaiseSeverityTo = !string.IsNullOrWhiteSpace(body.RaiseSeverityTo)
                ? Enum.Parse<RuleSeverity>(body.RaiseSeverityTo, true)
                : null,
            ClearRaiseSeverityTo = new ClearCriteria(body.ClearRaiseSeverityTo ?? false)
        };
    }
}
