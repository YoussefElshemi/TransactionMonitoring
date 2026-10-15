using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeReviewedAt
{
    public static ReviewedAt CreateValid(IFixture fixture)
    {
        return new ReviewedAt(fixture.Create<DateTimeOffset>());
    }
}
