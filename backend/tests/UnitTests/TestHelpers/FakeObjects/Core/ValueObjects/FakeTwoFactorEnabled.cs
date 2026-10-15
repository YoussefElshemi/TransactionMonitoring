using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeTwoFactorEnabled
{
    public static TwoFactorEnabled CreateValid(IFixture fixture)
    {
        return new TwoFactorEnabled(fixture.Create<bool>());
    }
}