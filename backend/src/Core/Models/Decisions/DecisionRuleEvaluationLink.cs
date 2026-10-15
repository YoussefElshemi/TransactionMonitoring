using Core.Models.BaseModels;
using Core.ValueObjects;

namespace Core.Models.Decisions;

public record DecisionRuleEvaluationLink : BaseModel
{
    public required DecisionId DecisionId { get; init; }
    public required RuleEvaluationId RuleEvaluationId { get; init; }
}
