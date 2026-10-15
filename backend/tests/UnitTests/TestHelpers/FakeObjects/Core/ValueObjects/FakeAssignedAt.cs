using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeAssignedAt
{
    public static AssignedAt CreateValid(IFixture fixture)
    {
        return new AssignedAt(fixture.Create<DateTimeOffset>());
    }
}