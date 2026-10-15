using Core.Enums;
using Core.Models.Common;

namespace Core.Models.Revenue;

public record ListServiceCostRateRequest : PagedRequest
{
    public required BillableResource? BillableResource { get; init; }
}
