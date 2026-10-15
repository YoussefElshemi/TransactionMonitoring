using Core.Models.Common;
using Core.Models.RateLimitOverrides;
using Core.ValueObjects;

namespace Core.Interfaces.Services;

public interface IRateLimitOverrideService
{
    Task<RateLimitOverride> CreateAsync(CreateRateLimitOverrideRequest createRateLimitOverrideRequest, CancellationToken stoppingToken);
    Task<RateLimitOverride> GetAsync(RateLimitOverrideId rateLimitOverrideId, CancellationToken stoppingToken);
    Task<PagedResponse<RateLimitOverride>> ListAsync(ListRateLimitOverrideRequest listRateLimitOverrideRequest, CancellationToken stoppingToken);
    Task<Exists> ExistsAsync(OrganizationId organizationId, RateLimitOverrideId rateLimitOverrideId, CancellationToken stoppingToken);
    Task<IReadOnlyCollection<RateLimitOverride>> ListActiveAsync(CancellationToken stoppingToken);
    Task DeleteAsync(RateLimitOverrideId rateLimitOverrideId, CancellationToken stoppingToken);
}
