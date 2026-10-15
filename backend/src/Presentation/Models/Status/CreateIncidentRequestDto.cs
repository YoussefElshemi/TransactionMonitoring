using Core.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Swashbuckle.AspNetCore.Annotations;

namespace Presentation.Models.Status;

public record CreateIncidentRequestDto
{
    [FromBody]
    public required CreateIncidentRequestBodyDto Body { get; init; }

    [BindNever]
    [SwaggerSchema(ReadOnly = true)]
    public Guid? CreatedByUserId { get; set; }
}

public record CreateIncidentRequestBodyDto
{
    public required string Title { get; init; }

    public required string Summary { get; init; }

    public required IncidentImpact Impact { get; init; }
    public required List<ServiceComponent> Components { get; init; }

    public DateTimeOffset? StartedAt { get; init; }
}
