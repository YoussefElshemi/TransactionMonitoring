using Core.Enums;
using Core.ValueObjects;

namespace Core.Models.Alerts;

public record AlertCandidate
{
    public required RuleId RuleId { get; init; }
    public required RuleVersionId RuleVersionId { get; init; }
    public required RuleEvaluationId RuleEvaluationId { get; init; }
    public required RuleSeverity Severity { get; init; }
    public required PartyId? PartyId { get; init; }
    public required PartyRole? PartyRole { get; init; }
    public required AlertPolicy AlertPolicy { get; init; }
}
