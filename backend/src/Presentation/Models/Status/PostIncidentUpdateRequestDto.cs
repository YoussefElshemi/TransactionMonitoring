using Core.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Swashbuckle.AspNetCore.Annotations;

namespace Presentation.Models.Status;

public record PostIncidentUpdateRequestDto
{
    [FromRoute]
    public required Guid IncidentId { get; init; }

    [FromBody]
    public required PostIncidentUpdateRequestBodyDto Body { get; init; }

    [BindNever]
    [SwaggerSchema(ReadOnly = true)]
    public Guid? PostedByUserId { get; set; }
}

public record PostIncidentUpdateRequestBodyDto
{
    public required IncidentStatus Status { get; init; }

    public required string Body { get; init; }
}
