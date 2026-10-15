using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeDueAt
{
    public static DueAt CreateValid(IFixture fixture)
    {
        return new DueAt(fixture.Create<DateTimeOffset>());
    }
}
