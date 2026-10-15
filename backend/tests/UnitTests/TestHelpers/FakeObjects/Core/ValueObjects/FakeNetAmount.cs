using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeNetAmount
{
    public static NetAmount CreateValid(IFixture fixture)
    {
        return new NetAmount(fixture.Create<decimal>());
    }
}
