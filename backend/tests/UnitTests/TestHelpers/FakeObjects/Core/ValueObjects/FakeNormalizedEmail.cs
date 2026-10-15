using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeNormalizedEmail
{
    public static NormalizedEmail CreateValid(IFixture fixture)
    {
        return new NormalizedEmail(fixture.Create<string>());
    }
}