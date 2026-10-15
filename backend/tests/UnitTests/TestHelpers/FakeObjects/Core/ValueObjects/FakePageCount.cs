using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakePageCount
{
    public static PageCount CreateValid(IFixture fixture)
    {
        return new PageCount(fixture.Create<int>());
    }
}