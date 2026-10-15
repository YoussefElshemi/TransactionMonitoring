using AutoFixture;
using Infrastructure.Entities;

namespace UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;

public static class FakePlanPriceTierEntity
{
    public static PlanPriceTierEntity CreateValid(IFixture fixture)
    {
        return new PlanPriceTierEntity
        {
            PlanPriceTierId = fixture.Create<Guid>(),
            PlanPriceId = fixture.Create<Guid>(),
            FromQuantity = 0,
            ToQuantity = 1000,
            UnitAmount = 0.02m,
            FlatAmount = 5m,
            CreatedAt = fixture.Create<DateTimeOffset>()
        };
    }
}
