namespace Infrastructure.Entities;

public record InvoiceLineEntity
{
    public required Guid InvoiceLineId { get; init; }
    public required Guid InvoiceId { get; init; }
    public InvoiceEntity Invoice { get; init; } = null!;
    public required int InvoiceLineTypeId { get; init; }
    public InvoiceLineTypeEntity InvoiceLineType { get; init; } = null!;
    public required int? BillableResourceId { get; init; }
    public BillableResourceEntity? BillableResource { get; init; }
    public required string Description { get; init; }
    public required int Quantity { get; init; }
    public required decimal UnitAmount { get; init; }
    public required decimal Amount { get; init; }
    public required Guid? InvoiceAdjustmentId { get; init; }
    public required int SortOrder { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}
