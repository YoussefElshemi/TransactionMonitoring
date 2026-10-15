using AutoFixture;
using Core.Enums;
using Core.Models.Invoices;
using Core.ValueObjects;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.Models.Invoices;

public static class FakeInvoiceLine
{
    public static InvoiceLine CreateValid(IFixture fixture)
    {
        return new InvoiceLine
        {
            InvoiceLineId = FakeInvoiceLineId.CreateValid(fixture),
            InvoiceId = FakeInvoiceId.CreateValid(fixture),
            InvoiceLineType = InvoiceLineType.Usage,
            BillableResource = BillableResource.RuleEvaluation,
            Description = FakeInvoiceLineDescription.CreateValid(fixture),
            Quantity = FakeQuantity.CreateValid(fixture),
            UnitAmount = FakeUnitAmount.CreateValid(fixture),
            Amount = FakeLineAmount.CreateValid(fixture),
            InvoiceAdjustmentId = null,
            SortOrder = new SortOrder(0),
            CreatedAt = FakeCreatedAt.CreateValid(fixture)
        };
    }
}
