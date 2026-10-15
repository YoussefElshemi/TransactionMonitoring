using AutoFixture;
using Core.Models.Subscriptions;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Plans;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.Models.Subscriptions;

public static class FakeSubscription
{
    public static Subscription CreateValid(IFixture fixture)
    {
        var planVersion = FakePlanVersion.CreateValid(fixture);

        return new Subscription
        {
            SubscriptionId = FakeSubscriptionId.CreateValid(fixture),
            OrganizationId = FakeOrganizationId.CreateValid(fixture),

            PlanVersionId = planVersion.PlanVersionId,
            PlanVersion = planVersion,
            Plan = FakePlan.CreateValid(fixture),
            EffectiveFrom = FakeEffectiveFrom.CreateValid(fixture),
            EffectiveTo = FakeEffectiveTo.CreateValid(fixture),
            CreatedByUserId = FakeApplicationUserId.CreateValid(fixture),
            CreatedAt = FakeCreatedAt.CreateValid(fixture),
            UpdatedAt = FakeUpdatedAt.CreateValid(fixture),
            IsDeleted = FakeIsDeleted.CreateValid(fixture),
            DeletedAt = FakeDeletedAt.CreateValid(fixture)
        };
    }
}
