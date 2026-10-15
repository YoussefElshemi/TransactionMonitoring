using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeLockoutEnd
{
    public static LockoutEnd CreateValid(IFixture fixture)
    {
        return new LockoutEnd(fixture.Create<DateTimeOffset>());
    }
}