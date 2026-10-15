using Microsoft.AspNetCore.Mvc;

namespace Presentation.Models.RateLimitOverrides;

public record OrganizationRateLimitOverrideRequestDto
{
    [FromRoute]
    public required Guid OrganizationId { get; init; }

    [FromRoute]
    public required Guid RateLimitOverrideId { get; init; }
}
