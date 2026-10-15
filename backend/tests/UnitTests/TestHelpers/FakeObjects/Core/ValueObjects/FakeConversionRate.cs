using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeConversionRate
{
    public static ConversionRate CreateValid(IFixture fixture)
    {
        return new ConversionRate(1m);
    }
}
