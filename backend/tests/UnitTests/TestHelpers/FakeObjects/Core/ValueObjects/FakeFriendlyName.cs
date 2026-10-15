using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeFriendlyName
{
    public static FriendlyName CreateValid(IFixture fixture)
    {
        return new FriendlyName(fixture.Create<string>());
    }
}