using Core.Models.Rules;

namespace Core.Mappers;

public static class RulePolicyMapper
{
    public static RulePolicy ToPolicy(this Rule rule)
    {
        return new RulePolicy
        {
            Action = rule.Action,
            Severity = rule.Severity,
            RiskWeight = rule.RiskWeight
        };
    }

    public static RulePolicy ToPolicy(this TestRuleRequest testRuleRequest)
    {
        return new RulePolicy
        {
            Action = testRuleRequest.Action,
            Severity = testRuleRequest.Severity,
            RiskWeight = testRuleRequest.RiskWeight
        };
    }

    public static RulePolicy ToPolicy(this RuleVersion ruleVersion)
    {
        return new RulePolicy
        {
            Action = ruleVersion.Action,
            Severity = ruleVersion.Severity,
            RiskWeight = ruleVersion.RiskWeight
        };
    }
}
