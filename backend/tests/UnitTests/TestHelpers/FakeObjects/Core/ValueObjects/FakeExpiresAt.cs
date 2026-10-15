using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeExpiresAt
{
    public static ExpiresAt CreateValid(IFixture fixture)
    {
        return new ExpiresAt(fixture.Create<DateTimeOffset>());
    }
}
