using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeRevokedAt
{
    public static RevokedAt CreateValid(IFixture fixture)
    {
        return new RevokedAt(fixture.Create<DateTimeOffset>());
    }
}
