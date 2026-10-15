using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeResolvedAt
{
    public static ResolvedAt CreateValid(IFixture fixture)
    {
        return new ResolvedAt(fixture.Create<DateTimeOffset>());
    }
}