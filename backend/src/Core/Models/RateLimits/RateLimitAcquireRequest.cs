namespace Core.Models.RateLimits;

public record RateLimitAcquireRequest
{
    public required IReadOnlyList<RateLimitBucket> Buckets { get; init; }
}
