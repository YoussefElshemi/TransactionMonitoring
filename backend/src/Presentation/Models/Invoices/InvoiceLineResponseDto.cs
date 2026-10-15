namespace Presentation.Models.Invoices;

public record InvoiceLineResponseDto
{
    public required Guid InvoiceLineId { get; init; }
    public required string InvoiceLineType { get; init; }
    public required string? BillableResource { get; init; }
    public required string Description { get; init; }
    public required int Quantity { get; init; }
    public required decimal UnitAmount { get; init; }

    public required decimal Amount { get; init; }

    public required Guid? InvoiceAdjustmentId { get; init; }
    public required int SortOrder { get; init; }
}
