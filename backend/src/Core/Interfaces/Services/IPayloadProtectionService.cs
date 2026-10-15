using Core.ValueObjects;

namespace Core.Interfaces.Services;

public interface IPayloadProtectionService
{
    Task<DataKey> GetOrCreateDataKeyAsync(OrganizationId organizationId, CancellationToken stoppingToken);

    // Must not mint: creating a key here would write on a read path.
    Task<DataKey?> FindDataKeyAsync(OrganizationId organizationId, CancellationToken stoppingToken);

    Task<IReadOnlyDictionary<OrganizationId, DataKey>> FindDataKeysAsync(
        IReadOnlyCollection<OrganizationId> organizationIds, CancellationToken stoppingToken);
}
