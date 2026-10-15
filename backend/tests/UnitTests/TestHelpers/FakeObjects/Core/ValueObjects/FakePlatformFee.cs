using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakePlatformFee
{
    public static PlatformFee CreateValid(IFixture fixture)
    {
        return new PlatformFee(fixture.Create<int>());
    }
}
