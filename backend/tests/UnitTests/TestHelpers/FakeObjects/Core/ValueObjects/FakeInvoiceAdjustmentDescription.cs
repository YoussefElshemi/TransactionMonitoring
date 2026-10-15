using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeInvoiceAdjustmentDescription
{
    public static InvoiceAdjustmentDescription CreateValid(IFixture fixture)
    {
        return new InvoiceAdjustmentDescription(fixture.Create<string>());
    }
}
