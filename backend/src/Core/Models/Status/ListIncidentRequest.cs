using Core.Enums;
using Core.Models.Common;
using Core.ValueObjects;

namespace Core.Models.Status;

public record ListIncidentRequest : PagedRequest
{
    public required ServiceComponent? Component { get; init; }
    public required IncidentStatus? Status { get; init; }

    public required IncludeResolved IncludeResolved { get; init; }
}
