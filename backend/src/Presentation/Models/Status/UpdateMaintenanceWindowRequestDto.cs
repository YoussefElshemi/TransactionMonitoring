using Core.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Models.Status;

public record UpdateMaintenanceWindowRequestDto
{
    [FromRoute]
    public required Guid MaintenanceWindowId { get; init; }

    [FromBody]
    public required UpdateMaintenanceWindowRequestBodyDto Body { get; init; }
}

public record UpdateMaintenanceWindowRequestBodyDto
{
    public required string Title { get; init; }
    public required string Description { get; init; }
    public required List<ServiceComponent> Components { get; init; }
    public required DateTimeOffset ScheduledStart { get; init; }
    public required DateTimeOffset ScheduledEnd { get; init; }
}
