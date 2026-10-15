using Core.Enums;
using Core.Models.BaseModels;
using Core.Models.Rules;
using Core.ValueObjects;

namespace Core.Models.RuleEvaluations;

public record RuleEvaluation : DeletableModel
{
    public required RuleEvaluationId RuleEvaluationId { get; init; }
    public required OrganizationId OrganizationId { get; init; }
    public required RuleId RuleId { get; init; }
    public required Rule? Rule { get; init; }
    public required RuleVersionId RuleVersionId { get; init; }
    public required RuleVersion? RuleVersion { get; init; }
    public required TransactionId TransactionId { get; init; }
    public required Matched Matched { get; init; }
    public required List<ConditionMatch>? ConditionMatches { get; init; }

    public required Mode Mode { get; init; }
}