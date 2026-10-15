using Core.Models.Rules;
using Presentation.Models.Rules;

namespace Presentation.Mappers.Rules;

public static class RuleOutcomeDtoMapper
{
    public static RulePolicyResponseDto ToDto(this RulePolicy rulePolicy)
    {
        return new RulePolicyResponseDto
        {
            Action = rulePolicy.Action.ToString(),
            Severity = rulePolicy.Severity.ToString(),
            RiskWeight = rulePolicy.RiskWeight
        };
    }

    public static RuleOutcomeResponseDto ToDto(this RuleOutcome ruleOutcome)
    {
        return new RuleOutcomeResponseDto
        {
            Action = ruleOutcome.Action.ToString(),
            Severity = ruleOutcome.Severity.ToString(),
            RiskScore = ruleOutcome.RiskScore
        };
    }
}
