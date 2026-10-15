using Microsoft.AspNetCore.Mvc;

namespace Presentation.Models.Plans;

public record UpdatePlanRequestDto
{
    [FromRoute]
    public required Guid PlanId { get; init; }

    [FromBody]
    public required UpdatePlanRequestBodyDto Body { get; init; }
}

public record UpdatePlanRequestBodyDto
{
    public string? Name { get; init; }
    public string? Description { get; init; }
    public bool? IsEnabled { get; init; }
}
