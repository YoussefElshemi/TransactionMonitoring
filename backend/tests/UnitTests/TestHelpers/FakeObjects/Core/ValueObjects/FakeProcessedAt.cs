using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeProcessedAt
{
    public static ProcessedAt CreateValid(IFixture fixture)
    {
        return new ProcessedAt(fixture.Create<DateTimeOffset>());
    }
}