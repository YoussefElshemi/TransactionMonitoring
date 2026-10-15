using Core.Models.PartyMappings;
using Core.ValueObjects;

namespace Core.Interfaces.Repositories;

public interface IPartyMappingVersionRepository : IPagedRepository<ListPartyMappingVersionRequest, PartyMappingVersion>
{
    Task AddAsync(PartyMappingVersion partyMappingVersion, CancellationToken stoppingToken);
    Task<PartyMappingVersion> GetByVersionNumberAsync(PartyMappingId partyMappingId, VersionNumber versionNumber, CancellationToken stoppingToken);
    Task<VersionNumber> GetMaxVersionNumberAsync(PartyMappingId partyMappingId, CancellationToken stoppingToken);
    Task<Exists> ExistsAsync(OrganizationId organizationId, VersionNumber versionNumber, CancellationToken stoppingToken);
}
