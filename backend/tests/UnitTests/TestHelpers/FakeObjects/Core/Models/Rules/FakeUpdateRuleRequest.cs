using AutoFixture;
using Core.Enums;
using Core.Models.Rules;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.Models.Rules;

public static class FakeUpdateRuleRequest
{
    public static UpdateRuleRequest CreateValid(IFixture fixture)
    {
        return new UpdateRuleRequest
        {
            RuleId = FakeRuleId.CreateValid(fixture),
            FriendlyName = FakeFriendlyName.CreateValid(fixture),
            IsEnabled = FakeIsEnabled.CreateValid(fixture),
            Action = RuleAction.Review,
            Severity = RuleSeverity.Medium,
            RiskWeight = FakeRiskWeight.CreateValid(fixture),
            HoldWindow = default,
            HoldExpiryVerdict = default,
            AlertSubjectRole = default,
            DeduplicationWindow = default
        };
    }
}
