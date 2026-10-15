using AutoFixture;
using Core.Models.RuleSets;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.Models.RuleSets;

public static class FakeCreateRuleSetRequest
{
    public static CreateRuleSetRequest CreateValid(IFixture fixture)
    {
        return new CreateRuleSetRequest
        {
            FriendlyName = FakeFriendlyName.CreateValid(fixture),
            SubOrganizationId = null,
            OrganizationId = FakeOrganizationId.CreateValid(fixture),
            IsEnabled = FakeIsEnabled.CreateValid(fixture),
            ReviewThreshold = FakeReviewThreshold.CreateValid(fixture),
            CreatedByUserId = FakeApplicationUserId.CreateValid(fixture)
        };
    }
}