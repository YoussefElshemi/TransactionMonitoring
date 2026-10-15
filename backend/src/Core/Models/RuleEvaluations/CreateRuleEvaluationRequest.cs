using System.Text.Json.Nodes;
using Core.ValueObjects;

namespace Core.Models.RuleEvaluations;

public record CreateRuleEvaluationRequest
{
    public required OrganizationId OrganizationId { get; init; }
    public required RuleId RuleId { get; init; }
    public required RuleVersionId RuleVersionId { get; init; }
    public required TransactionId TransactionId { get; init; }
    public required JsonObject Payload { get; init; }
    public required Matched Matched { get; init; }
    public required List<ConditionMatch>? ConditionMatches { get; init; }
}
