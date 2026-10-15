using AutoFixture;
using Core.Enums;
using Core.Models.Rules;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.Models.Rules;

public static class FakeRuleOutcome
{
    public static RuleOutcome CreateValid(IFixture fixture)
    {
        return new RuleOutcome
        {
            Action = RuleAction.Review,
            Severity = RuleSeverity.Medium,
            RiskScore = FakeRiskScore.CreateValid(fixture)
        };
    }
}
