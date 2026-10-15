using Core.Models.RuleEvaluations;
using Presentation.Mappers.Rules;
using Presentation.Models.RuleEvaluations;

namespace Presentation.Mappers.RuleEvaluations;

public static class RuleEvaluationDtoMapper
{
    public static RuleEvaluationResponseDto ToDto(this RuleEvaluation ruleEvaluation)
    {
        return new RuleEvaluationResponseDto
        {
            Mode = ruleEvaluation.Mode.ToString(),
            RuleEvaluationId = ruleEvaluation.RuleEvaluationId,
            TransactionId = ruleEvaluation.TransactionId,
            OrganizationId = ruleEvaluation.OrganizationId,
            RuleId = ruleEvaluation.RuleId,
            RuleVersion = ruleEvaluation.RuleVersion!.ToDto(),
            Matched = ruleEvaluation.Matched,
            ConditionMatches = ruleEvaluation.ConditionMatches?.Select(x => x.ToDto()).ToArray(),
            CreatedAt = ruleEvaluation.CreatedAt,
            UpdatedAt = ruleEvaluation.UpdatedAt
        };
    }
}
