using AutoFixture;
using Core.Enums;
using Core.Models.RuleSets;
using Core.ValueObjects;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.Models.RuleSets;

public static class FakeRuleSet
{
    public static RuleSet CreateValid(IFixture fixture)
    {
        return new RuleSet
        {
            Mode = Mode.Live,
            CreatedAt = FakeCreatedAt.CreateValid(fixture),
            UpdatedAt = FakeUpdatedAt.CreateValid(fixture),
            IsDeleted = FakeIsDeleted.CreateValid(fixture),
            DeletedAt = FakeDeletedAt.CreateValid(fixture),
            RuleSetId = FakeRuleSetId.CreateValid(fixture),
            FriendlyName = FakeFriendlyName.CreateValid(fixture),
            IsEnabled = FakeIsEnabled.CreateValid(fixture),
            ReviewThreshold = FakeReviewThreshold.CreateValid(fixture),
            SubOrganizationId = null,
            OrganizationId = FakeOrganizationId.CreateValid(fixture),
            CreatedByUserId = FakeApplicationUserId.CreateValid(fixture),
            RuleCount = new RuleCount(0),
            Rules = []
        };
    }
}