using Core.Models.BaseModels;
using Core.ValueObjects;

namespace Core.Models.Adjustments;

public record InvoiceAdjustment : DeletableModel
{
    public required InvoiceAdjustmentId InvoiceAdjustmentId { get; init; }
    public required OrganizationId OrganizationId { get; init; }
    public required Month Month { get; init; }
    public required Year Year { get; init; }
    public required InvoiceAdjustmentDescription Description { get; init; }

    public required AdjustmentAmount Amount { get; init; }

    public required InvoiceId? AppliedToInvoiceId { get; init; }

    public required ApplicationUserId? CreatedByUserId { get; init; }
}
