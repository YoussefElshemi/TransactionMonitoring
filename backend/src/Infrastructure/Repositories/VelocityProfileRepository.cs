using Core.Interfaces.Repositories;
using Core.Models.VelocityProfiles;
using Core.ValueObjects;
using Infrastructure.Entities;
using Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class VelocityProfileRepository(
    TransactionMonitoringDbContext context,
    TimeProvider timeProvider) : IVelocityProfileRepository
{
    public async Task AddAsync(VelocityProfile velocityProfile, CancellationToken stoppingToken)
    {
        await context.VelocityProfiles.AddAsync(velocityProfile.ToEntity(), stoppingToken);
    }

    public async Task ReplaceAsync(
        OrganizationId organizationId,
        List<VelocityMetric> metrics,
        List<VelocityThreshold> thresholds,
        CancellationToken stoppingToken)
    {
        var velocityProfileEntity = await context.VelocityProfiles
            .FirstAsync(x => x.OrganizationId == organizationId, stoppingToken);

        velocityProfileEntity.Metrics = metrics.ToBase64String();
        velocityProfileEntity.Thresholds = thresholds.ToBase64String();
        velocityProfileEntity.UpdatedAt = timeProvider.GetUtcNow();

        velocityProfileEntity.IsDeleted = false;
        velocityProfileEntity.DeletedAt = null;

        context.VelocityProfiles.Update(velocityProfileEntity);
    }

    public async Task<VelocityProfile?> FindAsync(OrganizationId organizationId, IncludeDeleted includeDeleted, CancellationToken stoppingToken)
    {
        var velocityProfileEntity = await Query(includeDeleted)
            .FirstOrDefaultAsync(x => x.OrganizationId == organizationId, stoppingToken);

        return velocityProfileEntity?.ToModel();
    }

    public async Task<Exists> ExistsAsync(OrganizationId organizationId, IncludeDeleted includeDeleted, CancellationToken stoppingToken)
    {
        var exists = await Query(includeDeleted)
            .AnyAsync(x => x.OrganizationId == organizationId, stoppingToken);

        return new Exists(exists);
    }

    public async Task DeleteAsync(OrganizationId organizationId, CancellationToken stoppingToken)
    {
        var velocityProfileEntity = await context.VelocityProfiles
            .FirstAsync(x => x.OrganizationId == organizationId && !x.IsDeleted, stoppingToken);

        velocityProfileEntity.IsDeleted = true;
        velocityProfileEntity.DeletedAt = timeProvider.GetUtcNow();
        velocityProfileEntity.UpdatedAt = timeProvider.GetUtcNow();

        context.VelocityProfiles.Update(velocityProfileEntity);
    }

    public async Task RecoverAsync(OrganizationId organizationId, CancellationToken stoppingToken)
    {
        var velocityProfileEntity = await context.VelocityProfiles
            .FirstAsync(x => x.OrganizationId == organizationId && x.IsDeleted, stoppingToken);

        velocityProfileEntity.IsDeleted = false;
        velocityProfileEntity.DeletedAt = null;
        velocityProfileEntity.UpdatedAt = timeProvider.GetUtcNow();

        context.VelocityProfiles.Update(velocityProfileEntity);
    }

    private IQueryable<VelocityProfileEntity> Query(IncludeDeleted includeDeleted)
    {
        return includeDeleted
            ? context.VelocityProfiles
            : context.VelocityProfiles.Where(x => !x.IsDeleted);
    }
}
