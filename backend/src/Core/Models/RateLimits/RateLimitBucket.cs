using Core.ValueObjects;

namespace Core.Models.RateLimits;

public record RateLimitBucket
{
    public required RateLimitKey Key { get; init; }
    public required PermitLimit PermitLimit { get; init; }
    public required TokensPerPeriod TokensPerPeriod { get; init; }
    public required ReplenishmentPeriod ReplenishmentPeriod { get; init; }
}
