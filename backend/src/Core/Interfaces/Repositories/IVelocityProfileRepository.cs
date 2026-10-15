using Core.Models.VelocityProfiles;
using Core.ValueObjects;

namespace Core.Interfaces.Repositories;

public interface IVelocityProfileRepository
{
    Task AddAsync(VelocityProfile velocityProfile, CancellationToken stoppingToken);
    Task ReplaceAsync(OrganizationId organizationId, List<VelocityMetric> metrics, List<VelocityThreshold> thresholds, CancellationToken stoppingToken);
    Task<VelocityProfile?> FindAsync(OrganizationId organizationId, IncludeDeleted includeDeleted, CancellationToken stoppingToken);
    Task<Exists> ExistsAsync(OrganizationId organizationId, IncludeDeleted includeDeleted, CancellationToken stoppingToken);
    Task DeleteAsync(OrganizationId organizationId, CancellationToken stoppingToken);
    Task RecoverAsync(OrganizationId organizationId, CancellationToken stoppingToken);
}
