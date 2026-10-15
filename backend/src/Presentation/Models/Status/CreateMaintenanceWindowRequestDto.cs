using Core.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Swashbuckle.AspNetCore.Annotations;

namespace Presentation.Models.Status;

public record CreateMaintenanceWindowRequestDto
{
    [FromBody]
    public required CreateMaintenanceWindowRequestBodyDto Body { get; init; }

    [BindNever]
    [SwaggerSchema(ReadOnly = true)]
    public Guid? CreatedByUserId { get; set; }
}

public record CreateMaintenanceWindowRequestBodyDto
{
    public required string Title { get; init; }
    public required string Description { get; init; }
    public required List<ServiceComponent> Components { get; init; }

    public required DateTimeOffset ScheduledStart { get; init; }

    public required DateTimeOffset ScheduledEnd { get; init; }
}
