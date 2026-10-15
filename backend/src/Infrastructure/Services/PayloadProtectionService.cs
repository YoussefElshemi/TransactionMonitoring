using System.Collections.Concurrent;
using Core.Configs;
using Core.Helpers;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Models.OrganizationDataKeys;
using Core.ValueObjects;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services;

public class PayloadProtectionService(
    IOrganizationDataKeyRepository organizationDataKeyRepository,
    IOptions<AppConfig> appConfig,
    TimeProvider timeProvider) : IPayloadProtectionService
{
    // Keyed on the wrapped key, not the organisation, so one host serving many databases (the behavioural suite) can't cross keys.
    private static readonly ConcurrentDictionary<string, DataKey> UnwrappedDataKeys = new(StringComparer.Ordinal);

    public async Task<DataKey> GetOrCreateDataKeyAsync(OrganizationId organizationId, CancellationToken stoppingToken)
    {
        var dataKey = await FindDataKeyAsync(organizationId, stoppingToken);

        if (dataKey.HasValue)
        {
            return dataKey.Value;
        }

        var now = timeProvider.GetUtcNow();
        var minted = DataKeyWrapper.Create();

        var winner = await organizationDataKeyRepository.AddIfAbsentAsync(new OrganizationDataKey
        {
            OrganizationId = organizationId,
            WrappedDataKey = new WrappedDataKey(DataKeyWrapper.Wrap(MasterKey, organizationId, minted)),
            CreatedAt = new CreatedAt(now),
            UpdatedAt = new UpdatedAt(now)
        }, stoppingToken);

        return Unwrap(winner);
    }

    public async Task<DataKey?> FindDataKeyAsync(OrganizationId organizationId, CancellationToken stoppingToken)
    {
        var organizationDataKey = await organizationDataKeyRepository.FindAsync(organizationId, stoppingToken);

        return organizationDataKey is null ? null : Unwrap(organizationDataKey);
    }

    public async Task<IReadOnlyDictionary<OrganizationId, DataKey>> FindDataKeysAsync(
        IReadOnlyCollection<OrganizationId> organizationIds, CancellationToken stoppingToken)
    {
        var distinct = organizationIds.Distinct().ToList();

        if (distinct.Count == 0)
        {
            return new Dictionary<OrganizationId, DataKey>();
        }

        var organizationDataKeys = await organizationDataKeyRepository.ListByOrganizationIdsAsync(distinct, stoppingToken);

        return organizationDataKeys.ToDictionary(x => x.OrganizationId, Unwrap);
    }

    private DataKey Unwrap(OrganizationDataKey organizationDataKey)
    {
        return UnwrappedDataKeys.GetOrAdd(
            organizationDataKey.WrappedDataKey,
            _ => DataKeyWrapper.Unwrap(MasterKey, organizationDataKey.OrganizationId, organizationDataKey.WrappedDataKey));
    }

    private byte[] MasterKey => DataKeyWrapper.ReadMasterKey(appConfig.Value.PayloadProtectionConfig.MasterKey);
}
