using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeMonth
{
    public static Month CreateValid(IFixture fixture)
    {
        return new Month(fixture.Create<int>());
    }
}