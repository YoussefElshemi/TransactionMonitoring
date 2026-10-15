using AutoFixture;
using Core.Enums;
using Infrastructure.Entities;

namespace UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;

public static class FakePlanPriceEntity
{
    public static PlanPriceEntity CreateValid(IFixture fixture)
    {
        return new PlanPriceEntity
        {
            PlanPriceId = fixture.Create<Guid>(),
            PlanVersionId = fixture.Create<Guid>(),
            BillableResourceId = (int)BillableResource.RuleEvaluation,
            PricingModelId = (int)PricingModel.Graduated,
            FreeAllowance = 500,
            CreatedAt = fixture.Create<DateTimeOffset>(),
            Tiers = [FakePlanPriceTierEntity.CreateValid(fixture)]
        };
    }
}
