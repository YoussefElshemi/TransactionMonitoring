using Core.Enums;
using Core.Models.Common;
using Core.ValueObjects;

namespace Core.Models.Status;

public record ListMaintenanceWindowRequest : PagedRequest
{
    public required ServiceComponent? Component { get; init; }
    public required MaintenanceStatus? Status { get; init; }
    public required IncludePast IncludePast { get; init; }
}
