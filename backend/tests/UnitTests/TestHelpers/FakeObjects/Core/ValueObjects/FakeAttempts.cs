using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeAttempts
{
    public static Attempts CreateValid(IFixture fixture)
    {
        return new Attempts(fixture.Create<int>());
    }
}