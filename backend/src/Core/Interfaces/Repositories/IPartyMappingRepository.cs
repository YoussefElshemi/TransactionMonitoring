using Core.Models.PartyMappings;
using Core.ValueObjects;

namespace Core.Interfaces.Repositories;

public interface IPartyMappingRepository
{
    Task AddAsync(PartyMapping partyMapping, CancellationToken stoppingToken);
    Task ReplaceMappingsAsync(OrganizationId organizationId, List<PartyRoleMapping> mappings, PartyMappingVersionId currentVersionId, VersionNumber currentVersionNumber, CancellationToken stoppingToken);
    Task<PartyMapping?> FindAsync(OrganizationId organizationId, IncludeDeleted includeDeleted, CancellationToken stoppingToken);
    Task<Exists> ExistsAsync(OrganizationId organizationId, IncludeDeleted includeDeleted, CancellationToken stoppingToken);
    Task DeleteAsync(OrganizationId organizationId, CancellationToken stoppingToken);
    Task RecoverAsync(OrganizationId organizationId, CancellationToken stoppingToken);
}
