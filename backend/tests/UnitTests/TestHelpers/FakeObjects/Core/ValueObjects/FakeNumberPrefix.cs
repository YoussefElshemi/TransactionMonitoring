using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeNumberPrefix
{
    public static NumberPrefix CreateValid(IFixture fixture)
    {
        return new NumberPrefix("INV");
    }
}
