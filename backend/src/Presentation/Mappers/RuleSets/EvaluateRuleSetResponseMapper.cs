using Core.Models.RuleSets;
using Presentation.Mappers.Cases;
using Presentation.Mappers.Decisions;
using Presentation.Mappers.Rules;
using Presentation.Models.RuleSets;

namespace Presentation.Mappers.RuleSets;

public static class EvaluateRuleSetResponseMapper
{
    public static EvaluateRuleSetResponseDto ToDto(this EvaluateRuleSetResponse evaluateRuleSetResponse)
    {
        return new EvaluateRuleSetResponseDto
        {
            RuleSetId = evaluateRuleSetResponse.RuleSetId,
            FriendlyName = evaluateRuleSetResponse.FriendlyName,
            Results = evaluateRuleSetResponse.Results.Select(x => x.ToNestedDto()).ToArray(),
            Outcome = evaluateRuleSetResponse.Outcome.ToDto(),
            TransactionId = evaluateRuleSetResponse.TransactionId,
            TransactionReference = evaluateRuleSetResponse.ExternalReference?.ToString(),
            Case = evaluateRuleSetResponse.Case?.ToDto(),
            Decision = evaluateRuleSetResponse.Decision?.ToDto()
        };
    }
}
