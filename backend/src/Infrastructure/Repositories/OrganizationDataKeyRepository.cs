using Core.Exceptions;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Models.OrganizationDataKeys;
using Core.ValueObjects;
using Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Repositories;

public class OrganizationDataKeyRepository(
    TransactionMonitoringDbContext context,
    IServiceScopeFactory scopeFactory) : IOrganizationDataKeyRepository
{
    public async Task<OrganizationDataKey?> FindAsync(OrganizationId organizationId, CancellationToken stoppingToken)
    {
        var organizationDataKeyEntity = await context.OrganizationDataKeys
            .FirstOrDefaultAsync(x => x.OrganizationId == organizationId, stoppingToken);

        return organizationDataKeyEntity?.ToModel();
    }

    public async Task<List<OrganizationDataKey>> ListByOrganizationIdsAsync(
        IReadOnlyCollection<OrganizationId> organizationIds, CancellationToken stoppingToken)
    {
        var ids = organizationIds.Select(x => (Guid)x).ToList();

        var organizationDataKeyEntities = await context.OrganizationDataKeys
            .Where(x => ids.Contains(x.OrganizationId))
            .ToListAsync(stoppingToken);

        return organizationDataKeyEntities.ConvertAll(x => x.ToModel());
    }

    public async Task<OrganizationDataKey> AddIfAbsentAsync(
        OrganizationDataKey organizationDataKey, CancellationToken stoppingToken)
    {
        using var scope = scopeFactory.CreateScope();
        var scopedContext = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();
        var scopedUnitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        await scopedContext.OrganizationDataKeys.AddAsync(organizationDataKey.ToEntity(), stoppingToken);

        try
        {
            await scopedUnitOfWork.SaveChangesAsync(stoppingToken);

            return organizationDataKey;
        }
        catch (UniqueConstraintViolationException)
        {
            return await FindAsync(organizationDataKey.OrganizationId, stoppingToken)
                   ?? throw new InvalidOperationException(
                       $"Organisation '{organizationDataKey.OrganizationId}' lost the race to mint a data key but no key is present.");
        }
    }
}
