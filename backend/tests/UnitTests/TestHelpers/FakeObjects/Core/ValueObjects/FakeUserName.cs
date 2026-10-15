using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeUserName
{
    public static UserName CreateValid(IFixture fixture)
    {
        return new UserName(fixture.Create<string>());
    }
}