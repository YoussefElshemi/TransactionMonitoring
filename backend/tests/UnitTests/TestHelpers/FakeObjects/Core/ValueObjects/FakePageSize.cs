using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakePageSize
{
    public const int Valid = 1;

    public static PageSize CreateValid(IFixture fixture)
    {
        return new PageSize(Valid);
    }
}