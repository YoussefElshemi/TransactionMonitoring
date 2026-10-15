using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeSecurityStamp
{
    public static SecurityStamp CreateValid(IFixture fixture)
    {
        return new SecurityStamp(fixture.Create<string>());
    }
}