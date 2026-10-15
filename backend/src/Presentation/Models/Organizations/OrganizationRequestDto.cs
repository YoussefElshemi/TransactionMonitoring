using Microsoft.AspNetCore.Mvc;

namespace Presentation.Models.Organizations;

public record OrganizationRequestDto
{
    [FromRoute]
    public required Guid OrganizationId { get; init; }
}