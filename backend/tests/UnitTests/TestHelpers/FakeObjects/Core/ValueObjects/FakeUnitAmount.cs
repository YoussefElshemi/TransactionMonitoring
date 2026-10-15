using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeUnitAmount
{
    public static UnitAmount CreateValid(IFixture fixture)
    {
        return new UnitAmount(fixture.Create<int>());
    }
}
