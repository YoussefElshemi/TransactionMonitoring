using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeMinimumCharge
{
    public static MinimumCharge CreateValid(IFixture fixture)
    {
        return new MinimumCharge(fixture.Create<int>());
    }
}
