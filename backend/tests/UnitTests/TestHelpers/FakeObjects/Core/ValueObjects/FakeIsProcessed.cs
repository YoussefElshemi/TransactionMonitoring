using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeIsProcessed
{
    public static IsProcessed CreateValid(IFixture fixture)
    {
        return new IsProcessed(fixture.Create<bool>());
    }
}