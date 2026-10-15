using Core.Enums;
using Core.Models.BaseModels;
using Core.Models.Rules;
using Core.ValueObjects;

namespace Core.Models.Escalations;

public record EscalationRule : DeletableModel
{
    public required EscalationRuleId EscalationRuleId { get; init; }
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

    public required Mode Mode { get; init; }
}
