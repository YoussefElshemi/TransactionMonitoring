using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeAccessFailedCount
{
    public static AccessFailedCount CreateValid(IFixture fixture)
    {
        return new AccessFailedCount(fixture.Create<int>());
    }
}