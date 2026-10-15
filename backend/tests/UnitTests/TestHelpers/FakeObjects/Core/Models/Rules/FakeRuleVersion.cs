using AutoFixture;
using Core.Enums;
using Core.Models.Rules;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.Models.Rules;

public static class FakeRuleVersion
{
    public static RuleVersion CreateValid(IFixture fixture)
    {
        return new RuleVersion
        {
            Mode = Mode.Live,
            RuleVersionId = FakeRuleVersionId.CreateValid(fixture),
            RuleId = FakeRuleId.CreateValid(fixture),
            VersionNumber = FakeVersionNumber.CreateValid(fixture),
            RuleBody = FakeRuleBody.CreateValid(fixture),
            MatchPolarity = RuleMatchPolarity.MatchesWhenTrue,
            Action = RuleAction.Review,
            Severity = RuleSeverity.Medium,
            RiskWeight = FakeRiskWeight.CreateValid(fixture),
            RuleSetId = FakeRuleSetId.CreateValid(fixture),
            OrganizationId = FakeOrganizationId.CreateValid(fixture),
            CreatedByUserId = FakeApplicationUserId.CreateValid(fixture),
            CreatedAt = FakeCreatedAt.CreateValid(fixture)
        };
    }
}
