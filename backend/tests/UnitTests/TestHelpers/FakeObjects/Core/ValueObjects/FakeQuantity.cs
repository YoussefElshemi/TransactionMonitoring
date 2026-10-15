using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeQuantity
{
    public static Quantity CreateValid(IFixture fixture)
    {
        return new Quantity(fixture.Create<int>());
    }
}
