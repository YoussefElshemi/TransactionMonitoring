using Core.Models.RateLimitOverrides;
using Core.ValueObjects;

namespace Core.Interfaces.Repositories;

public interface IRateLimitOverrideRepository : IPagedRepository<ListRateLimitOverrideRequest, RateLimitOverride>
{
    Task AddAsync(RateLimitOverride rateLimitOverride, CancellationToken stoppingToken);
    Task<RateLimitOverride> GetAsync(RateLimitOverrideId rateLimitOverrideId, CancellationToken stoppingToken);
    Task<Exists> ExistsAsync(OrganizationId organizationId, RateLimitOverrideId rateLimitOverrideId, CancellationToken stoppingToken);

    Task<IReadOnlyCollection<RateLimitOverride>> ListActiveAsync(CancellationToken stoppingToken);

    Task DeleteAsync(RateLimitOverrideId rateLimitOverrideId, CancellationToken stoppingToken);
}
