using Microsoft.AspNetCore.Mvc;
using Presentation.Models.Common;

namespace Presentation.Models.RateLimitOverrides;

public record ListRateLimitOverrideRequestDto : PagedRequestDto
{
    [FromRoute]
    public new required Guid OrganizationId
    {
        get => base.OrganizationId;
        init => base.OrganizationId = value;
    }

    public string? Pool { get; init; }
    public bool? IncludeExpired { get; init; }
}
