using Core.Enums;
using Presentation.Models.Common;

namespace Presentation.Models.Status;

public record ListMaintenanceWindowRequestDto : PagedRequestDto
{
    public ServiceComponent? Component { get; init; }
    public MaintenanceStatus? Status { get; init; }
    public bool? IncludePast { get; init; }
}
