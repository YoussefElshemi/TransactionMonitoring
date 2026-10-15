using Core.Enums;
using Core.Models.Rules;
using Core.ValueObjects;

namespace Core.Models.Escalations;

public record CreateEscalationRuleRequest
{
    public required OrganizationId OrganizationId { get; init; }
    public required EscalationRuleName Name { get; init; }
    public required EscalationRuleDescription? Description { get; init; }
    public required EscalationPriority Priority { get; init; }
    public required IsEnabled IsEnabled { get; init; }
    public required UnassignedFor? UnassignedFor { get; init; }
    public required HoldExpiryWithin? HoldExpiryWithin { get; init; }
    public required RuleSeverity? MinimumSeverity { get; init; }
    public required RuleBody? Matcher { get; init; }
    public required QueueId? TargetQueueId { get; init; }
    public required RuleSeverity? RaiseSeverityTo { get; init; }
}
