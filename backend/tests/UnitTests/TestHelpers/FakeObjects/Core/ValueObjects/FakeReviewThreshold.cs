using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeReviewThreshold
{
    public static ReviewThreshold CreateValid(IFixture fixture)
    {
        return new ReviewThreshold(Math.Abs(fixture.Create<int>()));
    }
}
