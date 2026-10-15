using Core.Models.BaseModels;
using Core.ValueObjects;

namespace Core.Models.Cases;

public record CaseRuleEvaluationLink : BaseModel
{
    public required CaseId CaseId { get; init; }
    public required RuleEvaluationId RuleEvaluationId { get; init; }
}