using AutoFixture;
using Core.Models.Subscriptions;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.Models.Subscriptions;

public static class FakeCreateSubscriptionRequest
{
    public static CreateSubscriptionRequest CreateValid(IFixture fixture)
    {
        return new CreateSubscriptionRequest
        {
            OrganizationId = FakeOrganizationId.CreateValid(fixture),
            PlanVersionId = FakePlanVersionId.CreateValid(fixture),
            EffectiveFrom = FakeEffectiveFrom.CreateValid(fixture),
            CreatedByUserId = FakeApplicationUserId.CreateValid(fixture)
        };
    }
}
