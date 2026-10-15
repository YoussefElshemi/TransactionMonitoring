using Microsoft.AspNetCore.Mvc;

namespace Presentation.Models.Status;

public record PublishIncidentReviewRequestDto
{
    [FromRoute]
    public required Guid IncidentId { get; init; }

    [FromBody]
    public required PublishIncidentReviewRequestBodyDto Body { get; init; }
}

public record PublishIncidentReviewRequestBodyDto
{
    public required string RootCause { get; init; }

    public required string Remediation { get; init; }
}
