using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakePlanName
{
    public static PlanName CreateValid(IFixture fixture)
    {
        return new PlanName(fixture.Create<string>());
    }
}
