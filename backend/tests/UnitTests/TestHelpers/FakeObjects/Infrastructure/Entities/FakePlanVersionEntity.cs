using AutoFixture;
using Core.Enums;
using Infrastructure.Entities;

namespace UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;

public static class FakePlanVersionEntity
{
    public static PlanVersionEntity CreateValid(IFixture fixture)
    {
        return new PlanVersionEntity
        {
            PlanVersionId = fixture.Create<Guid>(),
            PlanId = fixture.Create<Guid>(),
            VersionNumber = fixture.Create<int>(),
            CurrencyId = (int)Currency.Gbp,
            CostPerRequest = fixture.Create<decimal>(),
            PlatformFee = fixture.Create<decimal>(),
            MinimumCharge = fixture.Create<decimal>(),
            PublishedByUserId = fixture.Create<Guid>(),
            CreatedAt = fixture.Create<DateTimeOffset>()
        };
    }
}
