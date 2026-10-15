using Core.Enums;
using Core.ValueObjects;

namespace Core.Models.Invoices;

public record InvoiceLine
{
    public required InvoiceLineId InvoiceLineId { get; init; }
    public required InvoiceId InvoiceId { get; init; }
    public required InvoiceLineType InvoiceLineType { get; init; }

    public required BillableResource? BillableResource { get; init; }

    public required InvoiceLineDescription Description { get; init; }
    public required Quantity Quantity { get; init; }
    public required UnitAmount UnitAmount { get; init; }

    // Not always Quantity * UnitAmount: graduated bands, discounts and drawdowns compute their own amount.
    public required LineAmount Amount { get; init; }

    public required InvoiceAdjustmentId? InvoiceAdjustmentId { get; init; }

    public required SortOrder SortOrder { get; init; }

    public required CreatedAt CreatedAt { get; init; }
}
