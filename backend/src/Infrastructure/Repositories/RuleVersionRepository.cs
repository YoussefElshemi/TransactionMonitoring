using Core.Interfaces.Repositories;
using Core.Models.Common;
using Core.Models.Rules;
using Core.ValueObjects;
using Infrastructure.Entities;
using Infrastructure.Expressions;
using Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class RuleVersionRepository(
    TransactionMonitoringDbContext context) : PagedRepository<ListRuleVersionRequest, RuleVersionEntity, RuleVersion>, IRuleVersionRepository
{
    public async Task AddAsync(RuleVersion ruleVersion, CancellationToken stoppingToken)
    {
        var ruleVersionEntity = ruleVersion.ToEntity();
        await context.RuleVersions.AddAsync(ruleVersionEntity, stoppingToken);
    }

    public async Task<RuleVersion> GetAsync(RuleVersionId ruleVersionId, CancellationToken stoppingToken)
    {
        var ruleVersionEntity = await context.RuleVersions
            .FirstAsync(x => x.RuleVersionId == ruleVersionId, stoppingToken);

        return ruleVersionEntity.ToModel();
    }

    public async Task<List<RuleVersion>> ListByIdsAsync(IReadOnlyCollection<RuleVersionId> ruleVersionIds, CancellationToken stoppingToken)
    {
        if (ruleVersionIds.Count == 0)
        {
            return [];
        }

        var ruleVersionIdValues = ruleVersionIds.Select(x => (Guid)x).Distinct().ToList();

        var ruleVersionEntities = await context.RuleVersions
            .Where(x => ruleVersionIdValues.Contains(x.RuleVersionId))
            .ToListAsync(stoppingToken);

        return ruleVersionEntities.ConvertAll(x => x.ToModel());
    }

    public async Task<RuleVersion> GetByVersionNumberAsync(RuleId ruleId, VersionNumber versionNumber, CancellationToken stoppingToken)
    {
        var ruleVersionEntity = await context.RuleVersions
            .FirstAsync(x => x.RuleId == ruleId && x.VersionNumber == versionNumber, stoppingToken);

        return ruleVersionEntity.ToModel();
    }

    public async Task<VersionNumber> GetMaxVersionNumberAsync(RuleId ruleId, CancellationToken stoppingToken)
    {
        var maxVersionNumber = await context.RuleVersions
            .Where(x => x.RuleId == ruleId)
            .MaxAsync(x => (int?)x.VersionNumber, stoppingToken);

        return new VersionNumber(maxVersionNumber ?? 0);
    }

    public async Task<Exists> ExistsAsync(OrganizationId organizationId, RuleId ruleId, VersionNumber versionNumber, CancellationToken stoppingToken)
    {
        var exists = await context.RuleVersions
            .AnyAsync(x => x.OrganizationId == organizationId &&
                           x.RuleId == ruleId &&
                           x.VersionNumber == versionNumber, stoppingToken);

        return new Exists(exists);
    }

    protected override Task<List<RuleVersion>> MapPageAsync(List<RuleVersionEntity> items, ListRuleVersionRequest request, CancellationToken stoppingToken)
    {
        return Task.FromResult(items.ConvertAll(x => x.ToModel()));
    }

    protected override IOrderByExpression<RuleVersionEntity> OrderByExpression()
    {
        return new OrderByExpression<RuleVersionEntity, int>(x => x.VersionNumber);
    }

    protected override IQueryable<RuleVersionEntity> FilterByPagedRequest(ListRuleVersionRequest request)
    {
        return context.RuleVersions
            .Where(x => x.OrganizationId == request.OrganizationId &&
                        x.RuleId == request.RuleId);
    }

    protected override IQueryable<RuleVersionEntity> FilterByDateRange(IQueryable<RuleVersionEntity> queryable, DatedRequest datedRequest)
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
