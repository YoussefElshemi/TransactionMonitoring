using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Mappers;
using Core.Models.Common;
using Core.Models.RateLimitOverrides;
using Core.ValueObjects;

namespace Core.Services;

public class RateLimitOverrideService(
    IRateLimitOverrideRepository rateLimitOverrideRepository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IRateLimitOverrideService
{
    public async Task<RateLimitOverride> CreateAsync(CreateRateLimitOverrideRequest createRateLimitOverrideRequest, CancellationToken stoppingToken)
    {
        var rateLimitOverride = createRateLimitOverrideRequest.ToModel(timeProvider);

        await rateLimitOverrideRepository.AddAsync(rateLimitOverride, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);

        return rateLimitOverride;
    }

    public Task<RateLimitOverride> GetAsync(RateLimitOverrideId rateLimitOverrideId, CancellationToken stoppingToken)
    {
        return rateLimitOverrideRepository.GetAsync(rateLimitOverrideId, stoppingToken);
    }

    public Task<PagedResponse<RateLimitOverride>> ListAsync(ListRateLimitOverrideRequest listRateLimitOverrideRequest, CancellationToken stoppingToken)
    {
        return rateLimitOverrideRepository.ListAsync(listRateLimitOverrideRequest, stoppingToken);
    }

    public Task<Exists> ExistsAsync(OrganizationId organizationId, RateLimitOverrideId rateLimitOverrideId, CancellationToken stoppingToken)
    {
        return rateLimitOverrideRepository.ExistsAsync(organizationId, rateLimitOverrideId, stoppingToken);
    }

    public Task<IReadOnlyCollection<RateLimitOverride>> ListActiveAsync(CancellationToken stoppingToken)
    {
        return rateLimitOverrideRepository.ListActiveAsync(stoppingToken);
    }

    public async Task DeleteAsync(RateLimitOverrideId rateLimitOverrideId, CancellationToken stoppingToken)
    {
        await rateLimitOverrideRepository.DeleteAsync(rateLimitOverrideId, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);
    }
}
