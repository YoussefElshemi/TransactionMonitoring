using Core.Enums;
using Core.Models.Escalations;
using Core.ValueObjects;
using Presentation.Mappers.Rules;
using Presentation.Models.Escalations;

namespace Presentation.Mappers.Escalations;

public static class CreateEscalationRuleRequestMapper
{
    public static CreateEscalationRuleRequest ToModel(this CreateEscalationRuleRequestDto createEscalationRuleRequestDto)
    {
        var body = createEscalationRuleRequestDto.Body;

        return new CreateEscalationRuleRequest
        {
            OrganizationId = new OrganizationId(createEscalationRuleRequestDto.OrganizationId),
            Name = new EscalationRuleName(body.Name),
            Description = !string.IsNullOrWhiteSpace(body.Description) ? new EscalationRuleDescription(body.Description) : null,
            Priority = new EscalationPriority(body.Priority ?? EscalationPriority.MaxValue),
            IsEnabled = new IsEnabled(body.IsEnabled),
            UnassignedFor = body.UnassignedForMinutes.HasValue ? new UnassignedFor(body.UnassignedForMinutes.Value) : null,
            HoldExpiryWithin = body.HoldExpiryWithinMinutes.HasValue ? new HoldExpiryWithin(body.HoldExpiryWithinMinutes.Value) : null,
            MinimumSeverity = !string.IsNullOrWhiteSpace(body.MinimumSeverity)
                ? Enum.Parse<RuleSeverity>(body.MinimumSeverity, true)
                : null,
            Matcher = body.Matcher?.ToModel(),
            TargetQueueId = body.TargetQueueId.HasValue ? new QueueId(body.TargetQueueId.Value) : null,
            RaiseSeverityTo = !string.IsNullOrWhiteSpace(body.RaiseSeverityTo)
                ? Enum.Parse<RuleSeverity>(body.RaiseSeverityTo, true)
                : null
        };
    }
}
