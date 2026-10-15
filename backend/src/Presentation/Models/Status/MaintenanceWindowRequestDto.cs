using Microsoft.AspNetCore.Mvc;

namespace Presentation.Models.Status;

public record MaintenanceWindowRequestDto
{
    [FromRoute]
    public required Guid MaintenanceWindowId { get; init; }
}
