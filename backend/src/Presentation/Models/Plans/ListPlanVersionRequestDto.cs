using Microsoft.AspNetCore.Mvc;
using Presentation.Models.Common;

namespace Presentation.Models.Plans;

public record ListPlanVersionRequestDto : PagedRequestDto
{
    [FromRoute]
    public required Guid PlanId { get; init; }
}
