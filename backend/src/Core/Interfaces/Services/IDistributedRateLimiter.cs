using Core.Models.RateLimits;

namespace Core.Interfaces.Services;

public interface IDistributedRateLimiter
{
    Task<RateLimitDecision> AcquireAsync(RateLimitAcquireRequest rateLimitAcquireRequest, CancellationToken stoppingToken);
}
