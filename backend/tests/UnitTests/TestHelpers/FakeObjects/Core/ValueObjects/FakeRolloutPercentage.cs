using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeRolloutPercentage
{
    public static RolloutPercentage CreateValid(IFixture fixture)
    {
        return new RolloutPercentage(fixture.Create<int>() % 101);
    }
}
