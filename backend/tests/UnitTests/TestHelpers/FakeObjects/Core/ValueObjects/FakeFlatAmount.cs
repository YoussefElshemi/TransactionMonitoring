using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeFlatAmount
{
    public static FlatAmount CreateValid(IFixture fixture)
    {
        return new FlatAmount(fixture.Create<int>());
    }
}
