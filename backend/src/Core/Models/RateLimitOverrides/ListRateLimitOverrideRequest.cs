using Core.Enums;
using Core.Models.Common;
using Core.ValueObjects;

namespace Core.Models.RateLimitOverrides;

public record ListRateLimitOverrideRequest : PagedRequest
{
    public required OrganizationId OrganizationId { get; init; }
    public required RateLimitPool? Pool { get; init; }

    public required IncludeExpired IncludeExpired { get; init; }
}
