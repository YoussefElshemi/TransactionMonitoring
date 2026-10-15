using Core.Enums;
using Core.ValueObjects;

namespace Core.Models.RateLimits;

public record RateLimitDecision
{
    public required RateLimitOutcome Outcome { get; init; }

    public required PermitLimit? PermitLimit { get; init; }
    public required RemainingPermits? RemainingPermits { get; init; }
    public required RetryAfter? RetryAfter { get; init; }
}
