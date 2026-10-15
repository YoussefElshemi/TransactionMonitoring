using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeClosedAt
{
    public static ClosedAt CreateValid(IFixture fixture)
    {
        return new ClosedAt(fixture.Create<DateTimeOffset>());
    }
}
