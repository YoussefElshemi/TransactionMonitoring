namespace Presentation.RateLimiting;

public sealed record RateLimitResult
{
    public required bool IsAllowed { get; init; }
    public required long Limit { get; init; }
    public required long Remaining { get; init; }
    public required int ResetSeconds { get; init; }
    public required int RetryAfterSeconds { get; init; }
}
