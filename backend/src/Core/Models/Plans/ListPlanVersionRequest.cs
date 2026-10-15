using Core.Models.Common;
using Core.ValueObjects;

namespace Core.Models.Plans;

public record ListPlanVersionRequest : PagedRequest
{
    public required PlanId PlanId { get; init; }
}
