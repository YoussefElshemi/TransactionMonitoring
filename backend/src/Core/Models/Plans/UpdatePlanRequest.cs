using Core.Models.Common;
using Core.ValueObjects;

namespace Core.Models.Plans;

public record UpdatePlanRequest
{
    public required PlanId PlanId { get; init; }
    public required PlanName? Name { get; init; }
    public required Patch<PlanDescription?> Description { get; init; }
    public required IsEnabled? IsEnabled { get; init; }
}
