using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeCreatedAt
{
    public static CreatedAt CreateValid(IFixture fixture)
    {
        return new CreatedAt(fixture.Create<DateTimeOffset>());
    }
}