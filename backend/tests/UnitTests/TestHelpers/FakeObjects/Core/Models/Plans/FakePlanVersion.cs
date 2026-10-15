using AutoFixture;
using Core.Enums;
using Core.Models.Plans;
using Core.ValueObjects;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.Models.Plans;

public static class FakePlanVersion
{
    public static PlanVersion CreateValid(IFixture fixture)
    {
        var planVersionId = FakePlanVersionId.CreateValid(fixture);
        var costPerRequest = FakeCostPerRequest.CreateValid(fixture);

        return new PlanVersion
        {
            PlanVersionId = planVersionId,
            PlanId = FakePlanId.CreateValid(fixture),
            VersionNumber = FakeVersionNumber.CreateValid(fixture),
            Currency = Currency.Gbp,
            CostPerRequest = costPerRequest,
            PlatformFee = new PlatformFee(0),
            MinimumCharge = new MinimumCharge(0),

            Prices = [CreateFlatCatchAll(fixture, planVersionId, costPerRequest)],
            PublishedByUserId = FakeApplicationUserId.CreateValid(fixture),
            CreatedAt = FakeCreatedAt.CreateValid(fixture)
        };
    }

    public static PlanPrice CreateFlatCatchAll(IFixture fixture, PlanVersionId planVersionId, CostPerRequest costPerRequest)
    {
        var planPriceId = FakePlanPriceId.CreateValid(fixture);
        var createdAt = FakeCreatedAt.CreateValid(fixture);

        return new PlanPrice
        {
            PlanPriceId = planPriceId,
            PlanVersionId = planVersionId,
            BillableResource = null,
            PricingModel = PricingModel.Flat,
            FreeAllowance = new FreeAllowance(0),
            Tiers =
            [
                new PlanPriceTier
                {
                    PlanPriceTierId = FakePlanPriceTierId.CreateValid(fixture),
                    PlanPriceId = planPriceId,
                    FromQuantity = new FromQuantity(0),
                    ToQuantity = null,
                    UnitAmount = new UnitAmount(costPerRequest),
                    FlatAmount = new FlatAmount(0),
                    CreatedAt = createdAt
                }
            ],
            CreatedAt = createdAt
        };
    }
}
