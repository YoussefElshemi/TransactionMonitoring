using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakePlanPriceId
{
    public static PlanPriceId CreateValid(IFixture fixture)
    {
        return new PlanPriceId(fixture.Create<Guid>());
    }
}
