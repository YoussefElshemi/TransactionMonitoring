using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeSortOrder
{
    public static SortOrder CreateValid(IFixture fixture)
    {
        return new SortOrder(fixture.Create<int>());
    }
}
