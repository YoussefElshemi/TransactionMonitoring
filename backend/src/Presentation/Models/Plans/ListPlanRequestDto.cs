using Presentation.Models.Common;

namespace Presentation.Models.Plans;

public record ListPlanRequestDto : PagedRequestDto
{
    public string? Name { get; init; }

    public Guid? OwnerOrganizationId { get; init; }

    public bool? IncludeDisabled { get; init; }
}
