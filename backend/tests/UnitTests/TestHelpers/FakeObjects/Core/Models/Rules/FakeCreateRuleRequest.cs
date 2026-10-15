using AutoFixture;
using Core.Enums;
using Core.Models.Rules;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.Models.Rules;

public static class FakeCreateRuleRequest
{
    public static CreateRuleRequest CreateValid(IFixture fixture)
    {
        return new CreateRuleRequest
        {
            FriendlyName = FakeFriendlyName.CreateValid(fixture),
            RuleBody = FakeRuleBody.CreateValid(fixture),
            MatchPolarity = RuleMatchPolarity.MatchesWhenTrue,
            Action = RuleAction.Review,
            Severity = RuleSeverity.Medium,
            RiskWeight = FakeRiskWeight.CreateValid(fixture),
            HoldWindow = null,
            HoldExpiryVerdict = null,
            AlertSubjectRole = null,
            DeduplicationWindow = null,
            OrganizationId = FakeOrganizationId.CreateValid(fixture),
            IsEnabled = FakeIsEnabled.CreateValid(fixture),
            RuleSetId = FakeRuleSetId.CreateValid(fixture),
            CreatedByUserId = FakeApplicationUserId.CreateValid(fixture)
        };
    }
}