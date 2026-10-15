using Core.Enums;
using Core.Interfaces.Services;
using Core.Models.RateLimits;

namespace Infrastructure.Services;

public class DisabledDistributedRateLimiter : IDistributedRateLimiter
{
    public Task<RateLimitDecision> AcquireAsync(
        RateLimitAcquireRequest rateLimitAcquireRequest,
        CancellationToken stoppingToken)
    {
        return Task.FromResult(new RateLimitDecision
        {
            Outcome = RateLimitOutcome.Unavailable,
            PermitLimit = null,
            RemainingPermits = null,
            RetryAfter = null
        });
    }
}
