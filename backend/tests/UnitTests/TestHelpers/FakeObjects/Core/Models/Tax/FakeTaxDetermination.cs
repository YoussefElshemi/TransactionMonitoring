using AutoFixture;
using Core.Enums;
using Core.Models.Tax;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.Models.Tax;

public static class FakeTaxDetermination
{
    public static TaxDetermination CreateValid(IFixture fixture)
    {
        return new TaxDetermination
        {
            TaxTreatment = TaxTreatment.OutOfScope,
            TaxRate = new TaxRate(0m),
            Description = new InvoiceLineDescription("No VAT charged")
        };
    }

    public static TaxDetermination CreateStandard(decimal rate)
    {
        return new TaxDetermination
        {
            TaxTreatment = TaxTreatment.Standard,
            TaxRate = new TaxRate(rate),
            Description = new InvoiceLineDescription("VAT at " + rate + "%")
        };
    }
}
