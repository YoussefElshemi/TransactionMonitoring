using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeIssuedAt
{
    public static IssuedAt CreateValid(IFixture fixture)
    {
        return new IssuedAt(fixture.Create<DateTimeOffset>());
    }
}
