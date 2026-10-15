using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeRiskWeight
{
    public static RiskWeight CreateValid(IFixture fixture)
    {
        return new RiskWeight(fixture.Create<int>() % 101);
    }
}
