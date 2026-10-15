using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeAdjustmentAmount
{
    public static AdjustmentAmount CreateValid(IFixture fixture)
    {
        return new AdjustmentAmount(fixture.Create<int>());
    }
}
