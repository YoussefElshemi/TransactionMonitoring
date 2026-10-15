using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeHeldAt
{
    public static HeldAt CreateValid(IFixture fixture)
    {
        return new HeldAt(fixture.Create<DateTimeOffset>());
    }
}
