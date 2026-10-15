using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeUpdatedAt
{
    public static UpdatedAt CreateValid(IFixture fixture)
    {
        return new UpdatedAt(fixture.Create<DateTimeOffset>());
    }
}