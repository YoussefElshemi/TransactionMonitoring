using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakePlanPriceTierId
{
    public static PlanPriceTierId CreateValid(IFixture fixture)
    {
        return new PlanPriceTierId(fixture.Create<Guid>());
    }
}
