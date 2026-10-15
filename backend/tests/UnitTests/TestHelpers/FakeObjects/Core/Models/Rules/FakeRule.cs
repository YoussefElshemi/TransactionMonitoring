using AutoFixture;
using Core.Enums;
using Core.Models.Rules;
using Core.ValueObjects;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Organizations;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.Models.Rules;

public static class FakeRule
{
    public static Rule CreateValid(IFixture fixture)
    {
        return new Rule
        {
            Mode = Mode.Live,
            CreatedAt = FakeCreatedAt.CreateValid(fixture),
            UpdatedAt = FakeUpdatedAt.CreateValid(fixture),
            IsDeleted = FakeIsDeleted.CreateValid(fixture),
            DeletedAt = FakeDeletedAt.CreateValid(fixture),
            RuleId = FakeRuleId.CreateValid(fixture),
            RuleBody = FakeRuleBody.CreateValid(fixture),
            MatchPolarity = RuleMatchPolarity.MatchesWhenTrue,
            Action = RuleAction.Review,
            Severity = RuleSeverity.Medium,
            RiskWeight = FakeRiskWeight.CreateValid(fixture),
            HoldWindow = null,
            HoldExpiryVerdict = null,
            AlertSubjectRole = null,
            DeduplicationWindow = null,
            IsEnabled = FakeIsEnabled.CreateValid(fixture),
            RuleSetId = FakeRuleSetId.CreateValid(fixture),
            FriendlyName = FakeFriendlyName.CreateValid(fixture),
            OrganizationId = FakeOrganizationId.CreateValid(fixture),
            Organization = FakeOrganization.CreateValid(fixture),
            CreatedByUserId = FakeApplicationUserId.CreateValid(fixture),
            CurrentRuleVersionId = FakeRuleVersionId.CreateValid(fixture),
            CurrentVersionNumber = FakeVersionNumber.CreateValid(fixture),
            HasActiveDeployment = new HasActiveDeployment(false)
        };
    }
}