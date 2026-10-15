using Microsoft.AspNetCore.Mvc;

namespace Presentation.Models.Commitments;

public record CommitmentRequestDto
{
    [FromRoute]
    public required Guid OrganizationId { get; init; }

    [FromRoute]
    public required Guid CommitmentId { get; init; }
}
