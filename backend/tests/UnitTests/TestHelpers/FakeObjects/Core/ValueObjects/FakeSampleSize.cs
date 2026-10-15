using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeSampleSize
{
    public const int Valid = 50;

    public static SampleSize CreateValid(IFixture fixture)
    {
        return new SampleSize(Valid);
    }
}
