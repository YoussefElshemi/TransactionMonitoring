using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeToQuantity
{
    public static ToQuantity CreateValid(IFixture fixture)
    {
        return new ToQuantity(fixture.Create<int>());
    }
}
