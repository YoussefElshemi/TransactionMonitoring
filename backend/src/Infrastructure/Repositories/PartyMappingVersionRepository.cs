using Core.Interfaces.Repositories;
using Core.Models.Common;
using Core.Models.PartyMappings;
using Core.ValueObjects;
using Infrastructure.Entities;
using Infrastructure.Expressions;
using Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class PartyMappingVersionRepository(
    TransactionMonitoringDbContext context) : PagedRepository<ListPartyMappingVersionRequest, PartyMappingVersionEntity, PartyMappingVersion>, IPartyMappingVersionRepository
{
    public async Task AddAsync(PartyMappingVersion partyMappingVersion, CancellationToken stoppingToken)
    {
        var partyMappingVersionEntity = partyMappingVersion.ToEntity();
        await context.PartyMappingVersions.AddAsync(partyMappingVersionEntity, stoppingToken);
    }

    public async Task<PartyMappingVersion> GetByVersionNumberAsync(PartyMappingId partyMappingId, VersionNumber versionNumber, CancellationToken stoppingToken)
    {
        var partyMappingVersionEntity = await context.PartyMappingVersions
            .FirstAsync(x => x.PartyMappingId == partyMappingId && x.VersionNumber == versionNumber, stoppingToken);

        return partyMappingVersionEntity.ToModel();
    }

    public async Task<VersionNumber> GetMaxVersionNumberAsync(PartyMappingId partyMappingId, CancellationToken stoppingToken)
    {
        var maxVersionNumber = await context.PartyMappingVersions
            .Where(x => x.PartyMappingId == partyMappingId)
            .MaxAsync(x => (int?)x.VersionNumber, stoppingToken);

        return new VersionNumber(maxVersionNumber ?? 0);
    }

    public async Task<Exists> ExistsAsync(OrganizationId organizationId, VersionNumber versionNumber, CancellationToken stoppingToken)
    {
        var exists = await context.PartyMappingVersions
            .AnyAsync(x => x.OrganizationId == organizationId && x.VersionNumber == versionNumber, stoppingToken);

        return new Exists(exists);
    }

    protected override Task<List<PartyMappingVersion>> MapPageAsync(List<PartyMappingVersionEntity> items, ListPartyMappingVersionRequest request, CancellationToken stoppingToken)
    {
        return Task.FromResult(items.ConvertAll(x => x.ToModel()));
    }

    protected override IOrderByExpression<PartyMappingVersionEntity> OrderByExpression()
    {
        return new OrderByExpression<PartyMappingVersionEntity, int>(x => x.VersionNumber);
    }

    protected override IQueryable<PartyMappingVersionEntity> FilterByPagedRequest(ListPartyMappingVersionRequest request)
    {
        return context.PartyMappingVersions
            .Where(x => x.OrganizationId == request.OrganizationId);
    }

    protected override IQueryable<PartyMappingVersionEntity> FilterByDateRange(IQueryable<PartyMappingVersionEntity> queryable, DatedRequest datedRequest)
    {
        if (datedRequest.FromDate != DateOnly.MinValue)
        {
            queryable = queryable
                .Where(x => DateOnly.FromDateTime(x.CreatedAt.UtcDateTime) >= datedRequest.FromDate);
        }

        if (datedRequest.ToDate != DateOnly.MaxValue)
        {
            queryable = queryable
                .Where(x => DateOnly.FromDateTime(x.CreatedAt.UtcDateTime) <= datedRequest.ToDate);
        }

        return queryable;
    }
}
