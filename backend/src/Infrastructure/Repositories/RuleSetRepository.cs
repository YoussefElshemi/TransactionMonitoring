using Core.Interfaces.Repositories;
using Core.Models.Common;
using Core.Models.RuleSets;
using Core.ValueObjects;
using Infrastructure.Entities;
using Infrastructure.Expressions;
using Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class RuleSetRepository(
    TransactionMonitoringDbContext context,
    TimeProvider timeProvider) : PagedRepository<ListRuleSetRequest, RuleSetEntity, RuleSet>, IRuleSetRepository
{
    public async Task AddAsync(RuleSet ruleSet, CancellationToken stoppingToken)
    {
        var ruleSetEntity = ruleSet.ToEntity();
        await context.RuleSets.AddAsync(ruleSetEntity, stoppingToken);
    }

    public async Task UpdateAsync(UpdateRuleSetRequest updateRuleSetRequest, CancellationToken stoppingToken)
    {
        var ruleSetEntity = await context.RuleSets
            .FirstAsync(x => x.RuleSetId == updateRuleSetRequest.RuleSetId && !x.IsDeleted, stoppingToken);

        ruleSetEntity.FriendlyName = updateRuleSetRequest.FriendlyName ?? ruleSetEntity.FriendlyName;
        ruleSetEntity.IsEnabled = updateRuleSetRequest.IsEnabled ?? ruleSetEntity.IsEnabled;

        if (updateRuleSetRequest.ReviewThreshold.HasValue)
        {
            ruleSetEntity.ReviewThreshold = updateRuleSetRequest.ReviewThreshold.Value;
        }

        if (updateRuleSetRequest.ClearSubOrganization)
        {
            ruleSetEntity.SubOrganizationId = null;
        }
        else if (updateRuleSetRequest.SubOrganizationId.HasValue)
        {
            ruleSetEntity.SubOrganizationId = updateRuleSetRequest.SubOrganizationId.Value;
        }

        ruleSetEntity.UpdatedAt = timeProvider.GetUtcNow();

        context.RuleSets.Update(ruleSetEntity);
    }

    public async Task<Exists> ExistsAsync(OrganizationId organizationId, RuleSetId ruleSetId, IncludeDeleted includeDeleted, CancellationToken stoppingToken)
    {
        var exists = await context.RuleSets
            .AnyAsync(x => x.RuleSetId == ruleSetId &&
                           x.OrganizationId == organizationId &&
                           (includeDeleted || x.IsDeleted == false), stoppingToken);

        return new Exists(exists);
    }

    public async Task<RuleSet> GetAsync(RuleSetId ruleSetId, CancellationToken stoppingToken)
    {
        var ruleSetEntity = await context.RuleSets
            .Include(x => x.Rules.Where(rule => !rule.IsDeleted))
            .FirstAsync(x => x.RuleSetId == ruleSetId && !x.IsDeleted, stoppingToken);

        return ruleSetEntity.ToModel(ruleSetEntity.Rules.Count);
    }

    public async Task<List<RuleSet>> ListByIdsAsync(IReadOnlyCollection<RuleSetId> ruleSetIds, CancellationToken stoppingToken)
    {
        if (ruleSetIds.Count == 0)
        {
            return [];
        }

        var ruleSetIdValues = ruleSetIds.Select(x => (Guid)x).Distinct().ToList();

        var ruleSetEntities = await context.RuleSets
            .Include(x => x.Rules.Where(rule => !rule.IsDeleted))
            .Where(x => ruleSetIdValues.Contains(x.RuleSetId) && !x.IsDeleted)
            .ToListAsync(stoppingToken);

        return ruleSetEntities.ConvertAll(x => x.ToModel(x.Rules.Count));
    }

    public async Task<ReviewThreshold?> GetReviewThresholdAsync(RuleSetId ruleSetId, CancellationToken stoppingToken)
    {
        var reviewThreshold = await context.RuleSets
            .Where(x => x.RuleSetId == ruleSetId && !x.IsDeleted)
            .Select(x => x.ReviewThreshold)
            .FirstAsync(stoppingToken);

        return reviewThreshold.HasValue ? new ReviewThreshold(reviewThreshold.Value) : null;
    }

    public async Task<List<RuleSet>> GetAllAsync(OrganizationId organizationId, CancellationToken stoppingToken)
    {
        var ruleSetEntities = await context.RuleSets
            .Include(x => x.Rules.Where(rule => !rule.IsDeleted))
            .Where(x => x.OrganizationId == organizationId && !x.IsDeleted)
            .ToListAsync(stoppingToken);

        return ruleSetEntities.Select(x => x.ToModel(x.Rules.Count)).ToList();
    }

    public async Task DeleteAsync(RuleSetId ruleSetId, CancellationToken stoppingToken)
    {
        var ruleSetEntity = await context.RuleSets
            .FirstAsync(x => x.RuleSetId == ruleSetId && !x.IsDeleted, stoppingToken);

        ruleSetEntity.IsDeleted = true;
        ruleSetEntity.DeletedAt = timeProvider.GetUtcNow();

        context.RuleSets.Update(ruleSetEntity);
    }

    public async Task RecoverAsync(RuleSetId ruleSetId, CancellationToken stoppingToken)
    {
        var ruleSetEntity = await context.RuleSets
            .FirstAsync(x => x.RuleSetId == ruleSetId, stoppingToken);

        ruleSetEntity.IsDeleted = false;
        ruleSetEntity.DeletedAt = null;

        context.RuleSets.Update(ruleSetEntity);
    }

    protected override async Task<List<RuleSet>> MapPageAsync(List<RuleSetEntity> items, ListRuleSetRequest request, CancellationToken stoppingToken)
    {
        var ruleSetIds = items.ConvertAll(x => x.RuleSetId);

        var ruleCounts = await context.Rules
            .Where(x => ruleSetIds.Contains(x.RuleSetId) && !x.IsDeleted)
            .GroupBy(x => x.RuleSetId)
            .Select(x => new { RuleSetId = x.Key, Count = x.Count() })
            .ToDictionaryAsync(x => x.RuleSetId, x => x.Count, stoppingToken);

        return items.ConvertAll(x => x.ToModel(ruleCounts.GetValueOrDefault(x.RuleSetId)));
    }

    protected override IOrderByExpression<RuleSetEntity> OrderByExpression()
    {
        return new OrderByExpression<RuleSetEntity, DateTimeOffset>(x => x.UpdatedAt);
    }

    protected override IQueryable<RuleSetEntity> FilterByPagedRequest(ListRuleSetRequest listRuleSetRequest)
    {
        var queryable = context.RuleSets
            .Where(x => x.OrganizationId == listRuleSetRequest.OrganizationId && !x.IsDeleted)
            .ApplyPermissionFilter(listRuleSetRequest.PermissionFilter);

        if (listRuleSetRequest.FriendlyName.HasValue)
        {
            queryable = queryable
                .Where(x => EF.Functions.ILike(x.FriendlyName, $"%{listRuleSetRequest.FriendlyName}%"));
        }

        return queryable;
    }

    protected override IQueryable<RuleSetEntity> FilterByDateRange(IQueryable<RuleSetEntity> queryable, DatedRequest datedRequest)
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