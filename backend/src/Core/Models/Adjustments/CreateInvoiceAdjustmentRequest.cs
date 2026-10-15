using Core.ValueObjects;

namespace Core.Models.Adjustments;

public record CreateInvoiceAdjustmentRequest
{
    public required OrganizationId OrganizationId { get; init; }
    public required Month Month { get; init; }
    public required Year Year { get; init; }
    public required InvoiceAdjustmentDescription Description { get; init; }
    public required AdjustmentAmount Amount { get; init; }
    public required ApplicationUserId? CreatedByUserId { get; init; }
}
