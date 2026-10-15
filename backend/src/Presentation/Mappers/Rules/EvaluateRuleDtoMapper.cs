using Core.Models.Rules;
using Presentation.Mappers.Cases;
using Presentation.Mappers.Decisions;
using Presentation.Models.Rules;

namespace Presentation.Mappers.Rules;

public static class EvaluateRuleDtoMapper
{
    public static EvaluateRuleResponseDto ToDto(this EvaluateRuleResponse evaluateRuleResponse)
    {
        return Map(evaluateRuleResponse, isTopLevel: true);
    }

    public static EvaluateRuleResponseDto ToNestedDto(this EvaluateRuleResponse evaluateRuleResponse)
    {
        return Map(evaluateRuleResponse, isTopLevel: false);
    }

    private static EvaluateRuleResponseDto Map(EvaluateRuleResponse evaluateRuleResponse, bool isTopLevel)
    {
        return new EvaluateRuleResponseDto
        {
            RuleId = evaluateRuleResponse.RuleId,
            FriendlyName = evaluateRuleResponse.FriendlyName,
            Matched = evaluateRuleResponse.Matched,
            Policy = evaluateRuleResponse.Policy.ToDto(),
            Outcome = isTopLevel ? evaluateRuleResponse.Outcome.ToDto() : null,
            TransactionId = isTopLevel ? evaluateRuleResponse.TransactionId : null,
            TransactionReference = isTopLevel ? evaluateRuleResponse.ExternalReference?.ToString() : null,
            Case = evaluateRuleResponse.Case?.ToDto(),
            Decision = isTopLevel ? evaluateRuleResponse.Decision?.ToDto() : null
        };
    }
}
