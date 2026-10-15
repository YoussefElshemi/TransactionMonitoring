using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeDeletedAt
{
    public static DeletedAt CreateValid(IFixture fixture)
    {
        return new DeletedAt(fixture.Create<DateTimeOffset>());
    }
}