using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeYear
{
    public static Year CreateValid(IFixture fixture)
    {
        return new Year(fixture.Create<int>());
    }
}