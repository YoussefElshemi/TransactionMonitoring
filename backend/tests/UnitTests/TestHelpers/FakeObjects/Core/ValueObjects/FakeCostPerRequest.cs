using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeCostPerRequest
{
    public static CostPerRequest CreateValid(IFixture fixture)
    {
        return new CostPerRequest(fixture.Create<int>());
    }
}