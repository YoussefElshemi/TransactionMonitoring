using Core.Enums;
using Core.ValueObjects;

namespace Core.Models.Escalations;

public record CaseEscalation
{
    public required CaseId CaseId { get; init; }
    public required EscalationRuleId EscalationRuleId { get; init; }
    public required EscalatedAt EscalatedAt { get; init; }
    public required QueueId? TargetQueueId { get; init; }
    public required RuleSeverity? RaiseSeverityTo { get; init; }
}
