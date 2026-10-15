using Core.Models.PartyMappings;
using Core.Models.Rules;
using Core.ValueObjects;

namespace Core.Interfaces.Services;

public interface IPartyMappingService : IPagedService<ListPartyMappingVersionRequest, PartyMappingVersion>
{
    Task EnforceRuleBodyAsync(OrganizationId organizationId, RuleBodyModel ruleBody, CancellationToken stoppingToken);
    Task<List<PartyMappingUsage>> ListUsagesAsync(OrganizationId organizationId, CancellationToken stoppingToken);
    Task<PartyMapping> SetAsync(SetPartyMappingRequest setPartyMappingRequest, CancellationToken stoppingToken);
    Task<PartyMapping> GetAsync(OrganizationId organizationId, CancellationToken stoppingToken);
    Task<PartyMapping> GetIncludingDeletedAsync(OrganizationId organizationId, CancellationToken stoppingToken);
    Task<Exists> ExistsAsync(OrganizationId organizationId, IncludeDeleted includeDeleted, CancellationToken stoppingToken);
    Task DeleteAsync(OrganizationId organizationId, CancellationToken stoppingToken);
    Task RecoverAsync(OrganizationId organizationId, CancellationToken stoppingToken);
    Task<PartyMappingVersion> GetVersionAsync(OrganizationId organizationId, VersionNumber versionNumber, CancellationToken stoppingToken);
    Task<Exists> VersionExistsAsync(OrganizationId organizationId, VersionNumber versionNumber, CancellationToken stoppingToken);
}
