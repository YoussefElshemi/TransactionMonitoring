using Core.Interfaces.Repositories;
using Core.Models.PartyMappings;
using Core.ValueObjects;
using Infrastructure.Entities;
using Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class PartyMappingRepository(
    TransactionMonitoringDbContext context,
    TimeProvider timeProvider) : IPartyMappingRepository
{
    public async Task AddAsync(PartyMapping partyMapping, CancellationToken stoppingToken)
    {
        var partyMappingEntity = partyMapping.ToEntity();
        await context.PartyMappings.AddAsync(partyMappingEntity, stoppingToken);
    }

    public async Task ReplaceMappingsAsync(
        OrganizationId organizationId,
        List<PartyRoleMapping> mappings,
        PartyMappingVersionId currentVersionId,
        VersionNumber currentVersionNumber,
        CancellationToken stoppingToken)
    {
        var partyMappingEntity = await context.PartyMappings
            .FirstAsync(x => x.OrganizationId == organizationId, stoppingToken);

        partyMappingEntity.Mappings = mappings.ToBase64String();
        partyMappingEntity.CurrentVersionId = currentVersionId;
        partyMappingEntity.CurrentVersionNumber = currentVersionNumber;
        partyMappingEntity.UpdatedAt = timeProvider.GetUtcNow();

        partyMappingEntity.IsDeleted = false;
        partyMappingEntity.DeletedAt = null;

        context.PartyMappings.Update(partyMappingEntity);
    }

    public async Task<PartyMapping?> FindAsync(OrganizationId organizationId, IncludeDeleted includeDeleted, CancellationToken stoppingToken)
    {
        var partyMappingEntity = await Query(includeDeleted)
            .FirstOrDefaultAsync(x => x.OrganizationId == organizationId, stoppingToken);

        return partyMappingEntity?.ToModel();
    }

    public async Task<Exists> ExistsAsync(OrganizationId organizationId, IncludeDeleted includeDeleted, CancellationToken stoppingToken)
    {
        var exists = await Query(includeDeleted)
            .AnyAsync(x => x.OrganizationId == organizationId, stoppingToken);

        return new Exists(exists);
    }

    public async Task DeleteAsync(OrganizationId organizationId, CancellationToken stoppingToken)
    {
        var partyMappingEntity = await context.PartyMappings
            .FirstAsync(x => x.OrganizationId == organizationId && !x.IsDeleted, stoppingToken);

        partyMappingEntity.IsDeleted = true;
        partyMappingEntity.DeletedAt = timeProvider.GetUtcNow();
        partyMappingEntity.UpdatedAt = timeProvider.GetUtcNow();

        context.PartyMappings.Update(partyMappingEntity);
    }

    public async Task RecoverAsync(OrganizationId organizationId, CancellationToken stoppingToken)
    {
        var partyMappingEntity = await context.PartyMappings
            .FirstAsync(x => x.OrganizationId == organizationId && x.IsDeleted, stoppingToken);

        partyMappingEntity.IsDeleted = false;
        partyMappingEntity.DeletedAt = null;
        partyMappingEntity.UpdatedAt = timeProvider.GetUtcNow();

        context.PartyMappings.Update(partyMappingEntity);
    }

    private IQueryable<PartyMappingEntity> Query(IncludeDeleted includeDeleted)
    {
        return includeDeleted
            ? context.PartyMappings
            : context.PartyMappings.Where(x => !x.IsDeleted);
    }
}
