using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakePlanVersionId
{
    public static PlanVersionId CreateValid(IFixture fixture)
    {
        return new PlanVersionId(fixture.Create<Guid>());
    }
}
