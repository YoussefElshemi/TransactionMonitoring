using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeRiskScore
{
    public static RiskScore CreateValid(IFixture fixture)
    {
        return new RiskScore(Math.Abs(fixture.Create<int>()));
    }
}
