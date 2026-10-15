using Core.Models.OrganizationDataKeys;
using Core.ValueObjects;

namespace Core.Interfaces.Repositories;

public interface IOrganizationDataKeyRepository
{
    Task<OrganizationDataKey?> FindAsync(OrganizationId organizationId, CancellationToken stoppingToken);

    Task<List<OrganizationDataKey>> ListByOrganizationIdsAsync(
        IReadOnlyCollection<OrganizationId> organizationIds, CancellationToken stoppingToken);

    // Commits on its own and returns the winning row: concurrent first payments both try to mint a key, and the loser must adopt the winner's.
    Task<OrganizationDataKey> AddIfAbsentAsync(OrganizationDataKey organizationDataKey, CancellationToken stoppingToken);
}
