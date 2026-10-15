using AutoFixture;
using Core.Enums;
using Infrastructure.Entities;

namespace UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;

public static class FakeInvoiceLineEntity
{
    public static InvoiceLineEntity CreateValid(IFixture fixture)
    {
        return new InvoiceLineEntity
        {
            InvoiceLineId = fixture.Create<Guid>(),
            InvoiceId = fixture.Create<Guid>(),
            InvoiceLineTypeId = (int)InvoiceLineType.Usage,
            BillableResourceId = (int)BillableResource.RuleEvaluation,
            Description = fixture.Create<string>(),
            Quantity = 1000,
            UnitAmount = 0.02m,
            Amount = 20m,
            InvoiceAdjustmentId = null,
            SortOrder = 0,
            CreatedAt = fixture.Create<DateTimeOffset>()
        };
    }
}
