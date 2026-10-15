using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeTaxIdentifier
{
    public static TaxIdentifier CreateValid(IFixture fixture)
    {
        return new TaxIdentifier("GB123456789");
    }
}
