using Presentation.Models.Common;

namespace Presentation.Models.Dunning;

public record ListDunningStateRequestDto : PagedRequestDto
{
    public Guid? FilterOrganizationId { get; init; }

    public bool? IncludeResolved { get; init; }
}
