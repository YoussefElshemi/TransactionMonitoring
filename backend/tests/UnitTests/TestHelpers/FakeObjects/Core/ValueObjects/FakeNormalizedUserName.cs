using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeNormalizedUserName
{
    public static NormalizedUserName CreateValid(IFixture fixture)
    {
        return new NormalizedUserName(fixture.Create<string>());
    }
}