using Core.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Models.Status;

public record UpdateIncidentRequestDto
{
    [FromRoute]
    public required Guid IncidentId { get; init; }

    [FromBody]
    public required UpdateIncidentRequestBodyDto Body { get; init; }
}

public record UpdateIncidentRequestBodyDto
{
    public required string Title { get; init; }
    public required string Summary { get; init; }
    public required IncidentImpact Impact { get; init; }
    public required List<ServiceComponent> Components { get; init; }
    public required DateTimeOffset StartedAt { get; init; }
}
