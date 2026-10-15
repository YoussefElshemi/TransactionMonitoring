using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeDiscountValue
{
    public static DiscountValue CreateValid(IFixture fixture)
    {
        return new DiscountValue(fixture.Create<int>());
    }
}
