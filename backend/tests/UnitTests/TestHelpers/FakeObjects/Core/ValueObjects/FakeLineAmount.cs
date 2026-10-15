using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeLineAmount
{
    public static LineAmount CreateValid(IFixture fixture)
    {
        return new LineAmount(fixture.Create<int>());
    }
}
