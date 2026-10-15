using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakePageNumber
{
    public const int Valid = 1;

    public static PageNumber CreateValid(IFixture fixture)
    {
        return new PageNumber(Valid);
    }
}