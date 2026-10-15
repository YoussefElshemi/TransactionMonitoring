using Microsoft.AspNetCore.Mvc;

namespace Presentation.Models.Availability;

public record AvailabilityCommitmentRequestDto
{
    [FromRoute]
    public required Guid OrganizationId { get; init; }

    [FromRoute]
    public required Guid AvailabilityCommitmentId { get; init; }
}
