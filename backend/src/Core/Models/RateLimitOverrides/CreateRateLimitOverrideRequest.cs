using Core.Enums;
using Core.ValueObjects;

namespace Core.Models.RateLimitOverrides;

public record CreateRateLimitOverrideRequest
{
    public required OrganizationId OrganizationId { get; init; }
    public required RateLimitPool? Pool { get; init; }
    public required PermitLimit PermitLimit { get; init; }
    public required TokensPerPeriod TokensPerPeriod { get; init; }
    public required ReplenishmentPeriod ReplenishmentPeriod { get; init; }
    public required RateLimitOverrideReason Reason { get; init; }
    public required ExpiresAt ExpiresAt { get; init; }
    public required ApplicationUserId CreatedByUserId { get; init; }
}
