using Core.Models.RuleEvaluations;
using Presentation.Models.RuleEvaluations;

namespace Presentation.Mappers.RuleEvaluations;

public static class ConditionMatchDtoMapper
{
    public static ConditionMatchResponseDto ToDto(this ConditionMatch conditionMatch)
    {
        return new ConditionMatchResponseDto
        {
            Field = conditionMatch.Field,
            Condition = conditionMatch.Condition?.ToString(),
            Quantifier = conditionMatch.Quantifier?.ToString(),
            Expected = conditionMatch.Expected,
            Actual = conditionMatch.Actual,
            Matched = conditionMatch.Matched
        };
    }
}
