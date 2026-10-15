using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeSlaTarget
{
    public static SlaTarget CreateValid(IFixture fixture)
    {
        return new SlaTarget(fixture.Create<int>() % SlaTarget.MaxValue + 1);
    }
}
