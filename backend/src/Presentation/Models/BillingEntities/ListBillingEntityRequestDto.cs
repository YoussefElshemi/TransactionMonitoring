using Presentation.Models.Common;

namespace Presentation.Models.BillingEntities;

public record ListBillingEntityRequestDto : PagedRequestDto
{
    public bool? IncludeDisabled { get; init; }
}
