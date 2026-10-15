using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeTotalCount
{
    public static TotalCount CreateValid(IFixture fixture)
    {
        return new TotalCount(fixture.Create<int>());
    }
}