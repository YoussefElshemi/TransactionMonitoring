using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeFromQuantity
{
    public static FromQuantity CreateValid(IFixture fixture)
    {
        return new FromQuantity(fixture.Create<int>());
    }
}
