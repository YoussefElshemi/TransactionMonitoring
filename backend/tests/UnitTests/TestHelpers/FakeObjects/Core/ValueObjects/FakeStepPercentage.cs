using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeStepPercentage
{
    public static StepPercentage CreateValid(IFixture fixture)
    {
        return new StepPercentage(fixture.Create<int>() % 100 + 1);
    }
}
