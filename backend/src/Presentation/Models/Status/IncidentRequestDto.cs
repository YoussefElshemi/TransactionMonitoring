using Microsoft.AspNetCore.Mvc;

namespace Presentation.Models.Status;

public record IncidentRequestDto
{
    [FromRoute]
    public required Guid IncidentId { get; init; }
}
