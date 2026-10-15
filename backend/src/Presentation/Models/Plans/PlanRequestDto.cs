using Microsoft.AspNetCore.Mvc;

namespace Presentation.Models.Plans;

public record PlanRequestDto
{
    [FromRoute]
    public required Guid PlanId { get; init; }
}
