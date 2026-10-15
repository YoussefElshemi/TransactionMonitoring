using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeMatched
{
    public static Matched CreateValid(IFixture fixture)
    {
        return new Matched(fixture.Create<bool>());
    }
}
