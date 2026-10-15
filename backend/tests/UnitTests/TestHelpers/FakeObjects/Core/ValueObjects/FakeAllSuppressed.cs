using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeAllSuppressed
{
    public static AllSuppressed CreateValid(IFixture fixture)
    {
        return new AllSuppressed(fixture.Create<bool>());
    }
}
