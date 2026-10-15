using Core.Enums;
using Core.Models.Invoices;
using Core.ValueObjects;
using Infrastructure.Entities;

namespace Infrastructure.Mappers;

public static class InvoiceLineMapper
{
    public static InvoiceLine ToModel(this InvoiceLineEntity invoiceLineEntity)
    {
        return new InvoiceLine
        {
            InvoiceLineId = new InvoiceLineId(invoiceLineEntity.InvoiceLineId),
            InvoiceId = new InvoiceId(invoiceLineEntity.InvoiceId),
            InvoiceLineType = (InvoiceLineType)invoiceLineEntity.InvoiceLineTypeId,
            BillableResource = invoiceLineEntity.BillableResourceId.HasValue ? (BillableResource)invoiceLineEntity.BillableResourceId.Value : null,
            Description = new InvoiceLineDescription(invoiceLineEntity.Description),
            Quantity = new Quantity(invoiceLineEntity.Quantity),
            UnitAmount = new UnitAmount(invoiceLineEntity.UnitAmount),
            Amount = new LineAmount(invoiceLineEntity.Amount),
            InvoiceAdjustmentId = invoiceLineEntity.InvoiceAdjustmentId.HasValue ? new InvoiceAdjustmentId(invoiceLineEntity.InvoiceAdjustmentId.Value) : null,
            SortOrder = new SortOrder(invoiceLineEntity.SortOrder),
            CreatedAt = new CreatedAt(invoiceLineEntity.CreatedAt)
        };
    }

    public static InvoiceLineEntity ToEntity(this InvoiceLine invoiceLine)
    {
        return new InvoiceLineEntity
        {
            InvoiceLineId = invoiceLine.InvoiceLineId,
            InvoiceId = invoiceLine.InvoiceId,
            InvoiceLineTypeId = (int)invoiceLine.InvoiceLineType,
            BillableResourceId = invoiceLine.BillableResource.HasValue ? (int)invoiceLine.BillableResource.Value : null,
            Description = invoiceLine.Description,
            Quantity = invoiceLine.Quantity,
            UnitAmount = invoiceLine.UnitAmount,
            Amount = invoiceLine.Amount,
            InvoiceAdjustmentId = invoiceLine.InvoiceAdjustmentId.HasValue ? invoiceLine.InvoiceAdjustmentId.Value : null,
            SortOrder = invoiceLine.SortOrder,
            CreatedAt = invoiceLine.CreatedAt
        };
    }
}
