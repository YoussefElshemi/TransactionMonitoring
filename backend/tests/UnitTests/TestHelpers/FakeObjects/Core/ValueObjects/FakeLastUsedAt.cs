using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeLastUsedAt
{
    public static LastUsedAt CreateValid(IFixture fixture)
    {
        return new LastUsedAt(fixture.Create<DateTimeOffset>());
    }
}
