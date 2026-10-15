using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakePlanDescription
{
    public static PlanDescription CreateValid(IFixture fixture)
    {
        return new PlanDescription(fixture.Create<string>());
    }
}
