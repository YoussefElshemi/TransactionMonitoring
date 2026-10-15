using AutoFixture;
using Core.Enums;
using Core.Models.Plans;
using Core.ValueObjects;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.Models.Plans;

public static class FakePublishPlanVersionRequest
{
    public static PublishPlanVersionRequest CreateValid(IFixture fixture)
    {
        return new PublishPlanVersionRequest
        {
            PlanId = FakePlanId.CreateValid(fixture),
            CostPerRequest = FakeCostPerRequest.CreateValid(fixture),
            Currency = Currency.Gbp,
            PlatformFee = new PlatformFee(0),
            MinimumCharge = new MinimumCharge(0),
            Prices = [],
            PublishedByUserId = FakeApplicationUserId.CreateValid(fixture)
        };
    }
}
