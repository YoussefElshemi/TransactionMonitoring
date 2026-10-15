using Core.Enums;
using Presentation.Models.Common;

namespace Presentation.Models.Status;

public record ListIncidentRequestDto : PagedRequestDto
{
    public ServiceComponent? Component { get; init; }
    public IncidentStatus? Status { get; init; }
    public bool? IncludeResolved { get; init; }
}
