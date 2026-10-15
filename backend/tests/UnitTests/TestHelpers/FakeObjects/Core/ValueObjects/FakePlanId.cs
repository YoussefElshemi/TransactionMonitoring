using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakePlanId
{
    public static PlanId CreateValid(IFixture fixture)
    {
        return new PlanId(fixture.Create<Guid>());
    }
}
