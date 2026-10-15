using Core.Models.Rules;
using Core.Models.VelocityProfiles;
using Core.ValueObjects;

namespace Core.Interfaces.Services;

public interface IVelocityProfileService : IPagedService<ListVelocityBackfillRequest, VelocityBackfill>
{
    Task EnforceRuleBodyAsync(OrganizationId organizationId, RuleBodyModel ruleBody, CancellationToken stoppingToken);
    Task<List<VelocityProfileUsage>> ListUsagesAsync(OrganizationId organizationId, CancellationToken stoppingToken);
    Task<VelocityProfile> SetAsync(SetVelocityProfileRequest setVelocityProfileRequest, CancellationToken stoppingToken);
    Task<VelocityProfile> GetAsync(OrganizationId organizationId, CancellationToken stoppingToken);
    Task<VelocityProfile> GetIncludingDeletedAsync(OrganizationId organizationId, CancellationToken stoppingToken);
    Task<Exists> ExistsAsync(OrganizationId organizationId, IncludeDeleted includeDeleted, CancellationToken stoppingToken);
    Task DeleteAsync(OrganizationId organizationId, CancellationToken stoppingToken);
    Task RecoverAsync(OrganizationId organizationId, CancellationToken stoppingToken);
    Task<VelocityBackfill> GetBackfillAsync(VelocityBackfillId velocityBackfillId, CancellationToken stoppingToken);
    Task<Exists> BackfillExistsAsync(OrganizationId organizationId, VelocityBackfillId velocityBackfillId, CancellationToken stoppingToken);
}
