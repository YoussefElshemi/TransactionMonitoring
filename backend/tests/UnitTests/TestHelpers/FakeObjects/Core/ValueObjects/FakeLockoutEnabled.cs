using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeLockoutEnabled
{
    public static LockoutEnabled CreateValid(IFixture fixture)
    {
        return new LockoutEnabled(fixture.Create<bool>());
    }
}