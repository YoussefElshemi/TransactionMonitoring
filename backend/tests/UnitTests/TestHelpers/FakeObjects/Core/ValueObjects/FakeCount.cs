using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeCount
{
    public static Count CreateValid(IFixture fixture)
    {
        return new Count(fixture.Create<int>());
    }
}