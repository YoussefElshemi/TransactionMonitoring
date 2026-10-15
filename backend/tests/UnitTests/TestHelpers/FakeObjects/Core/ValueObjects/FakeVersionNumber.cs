using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeVersionNumber
{
    public static VersionNumber CreateValid(IFixture fixture)
    {
        return new VersionNumber(fixture.Create<int>());
    }
}
