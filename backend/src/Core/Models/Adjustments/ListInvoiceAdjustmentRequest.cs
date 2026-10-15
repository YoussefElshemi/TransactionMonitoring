using Core.Models.Common;
using Core.ValueObjects;

namespace Core.Models.Adjustments;

public record ListInvoiceAdjustmentRequest : PagedRequest
{
    public required OrganizationId OrganizationId { get; init; }

    public required IncludeApplied IncludeApplied { get; init; }
}
