using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeTaxRate
{
    public static TaxRate CreateValid(IFixture fixture)
    {
        return new TaxRate(20m);
    }
}
