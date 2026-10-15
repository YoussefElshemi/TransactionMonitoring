using Core.Models.Common;
using Core.ValueObjects;

namespace Core.Models.Plans;

public record ListPlanRequest : PagedRequest
{
    public required PlanName? Name { get; init; }

    public required OrganizationId? OrganizationId { get; init; }

    public required IncludeDisabled IncludeDisabled { get; init; }
}
