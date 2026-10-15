using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeTaxAmount
{
    public static TaxAmount CreateValid(IFixture fixture)
    {
        return new TaxAmount(fixture.Create<decimal>());
    }
}
