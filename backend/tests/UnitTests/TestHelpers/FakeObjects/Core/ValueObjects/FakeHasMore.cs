using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeHasMore
{
    public static HasMore CreateValid(IFixture fixture)
    {
        return new HasMore(fixture.Create<bool>());
    }
}