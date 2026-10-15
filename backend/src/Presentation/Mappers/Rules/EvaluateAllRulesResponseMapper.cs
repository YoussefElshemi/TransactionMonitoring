using Core.Models.Rules;
using Presentation.Mappers.Cases;
using Presentation.Mappers.Decisions;
using Presentation.Models.Rules;

namespace Presentation.Mappers.Rules;

public static class EvaluateAllRulesResponseMapper
{
    public static EvaluateAllRulesResponseDto ToDto(this EvaluateAllRulesResponse evaluateAllRulesResponse)
    {
        return new EvaluateAllRulesResponseDto
        {
            Results = evaluateAllRulesResponse.Results.Select(x => x.ToNestedDto()).ToArray(),
            Outcome = evaluateAllRulesResponse.Outcome.ToDto(),
            TransactionId = evaluateAllRulesResponse.TransactionId,
            TransactionReference = evaluateAllRulesResponse.ExternalReference?.ToString(),
            Case = evaluateAllRulesResponse.Case?.ToDto(),
            Decision = evaluateAllRulesResponse.Decision?.ToDto()
        };
    }
}
