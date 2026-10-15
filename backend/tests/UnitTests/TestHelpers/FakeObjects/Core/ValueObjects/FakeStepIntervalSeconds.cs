using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeStepIntervalSeconds
{
    public static StepIntervalSeconds CreateValid(IFixture fixture)
    {
        return new StepIntervalSeconds(fixture.Create<int>());
    }
}
