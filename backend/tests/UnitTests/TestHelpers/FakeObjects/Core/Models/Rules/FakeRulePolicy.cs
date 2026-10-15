using AutoFixture;
using Core.Enums;
using Core.Models.Rules;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.Models.Rules;

public static class FakeRulePolicy
{
    public static RulePolicy CreateValid(IFixture fixture)
    {
        return new RulePolicy
        {
            Action = RuleAction.Review,
            Severity = RuleSeverity.Medium,
            RiskWeight = FakeRiskWeight.CreateValid(fixture)
        };
    }
}
