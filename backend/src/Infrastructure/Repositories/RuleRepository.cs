using Core.Interfaces.Repositories;
using Core.Models.Common;
using Core.Models.Rules;
using Core.ValueObjects;
using Infrastructure.Entities;
using Infrastructure.Expressions;
using Infrastructure.Extensions;
using Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class RuleRepository(
    TransactionMonitoringDbContext context,
    TimeProvider timeProvider) : PagedRepository<ListRuleRequest, RuleEntity, Rule>, IRuleRepository
{
    public async Task AddAsync(Rule rule, CancellationToken stoppingToken)
    {
        var ruleEntity = rule.ToEntity();
        await context.Rules.AddAsync(ruleEntity, stoppingToken);
    }

    public async Task UpdateAsync(UpdateRuleRequest updateRuleRequest, CancellationToken stoppingToken)
    {
        var ruleEntity = await context.Rules
            .FirstAsync(x => x.RuleId == updateRuleRequest.RuleId && !x.IsDeleted, stoppingToken);

        if (updateRuleRequest.FriendlyName.HasValue)
        {
            ruleEntity.FriendlyName = updateRuleRequest.FriendlyName.Value.ToString();
        }

        if (updateRuleRequest.IsEnabled.HasValue)
        {
            ruleEntity.IsEnabled = updateRuleRequest.IsEnabled.Value;
        }

        if (updateRuleRequest.Action.HasValue)
        {
            ruleEntity.ActionId = (int)updateRuleRequest.Action.Value;
        }

        if (updateRuleRequest.Severity.HasValue)
        {
            ruleEntity.SeverityId = (int)updateRuleRequest.Severity.Value;
        }

        if (updateRuleRequest.RiskWeight.HasValue)
        {
            ruleEntity.RiskWeight = updateRuleRequest.RiskWeight.Value;
        }

        if (updateRuleRequest.HoldWindow.IsSet)
        {
            ruleEntity.HoldWindow = updateRuleRequest.HoldWindow.Value;
        }

        if (updateRuleRequest.HoldExpiryVerdict.IsSet)
        {
            ruleEntity.HoldExpiryVerdictId = (int?)updateRuleRequest.HoldExpiryVerdict.Value;
        }

        if (updateRuleRequest.AlertSubjectRole.IsSet)
        {
            ruleEntity.AlertSubjectRoleId = (int?)updateRuleRequest.AlertSubjectRole.Value;
        }

        if (updateRuleRequest.DeduplicationWindow.IsSet)
        {
            ruleEntity.DeduplicationWindow = updateRuleRequest.DeduplicationWindow.Value;
        }

        ruleEntity.UpdatedAt = timeProvider.GetUtcNow();

        context.Rules.Update(ruleEntity);
    }

    public async Task DeployVersionAsync(RuleId ruleId, RuleVersion targetRuleVersion, CancellationToken stoppingToken)
    {
        var ruleEntity = await context.Rules
            .ForUpdate()
            .FirstAsync(x => x.RuleId == ruleId && !x.IsDeleted, stoppingToken);

        ruleEntity.RuleBody = targetRuleVersion.RuleBody.ToBase64String();
        ruleEntity.MatchPolarityId = (int)targetRuleVersion.MatchPolarity;
        ruleEntity.ActionId = (int)targetRuleVersion.Action;
        ruleEntity.SeverityId = (int)targetRuleVersion.Severity;
        ruleEntity.RiskWeight = targetRuleVersion.RiskWeight;
        ruleEntity.CurrentRuleVersionId = targetRuleVersion.RuleVersionId;
        ruleEntity.CurrentVersionNumber = targetRuleVersion.VersionNumber;
        ruleEntity.UpdatedAt = timeProvider.GetUtcNow();
    }

    public async Task MoveAsync(RuleId ruleId, RuleSetId targetRuleSetId, CancellationToken stoppingToken)
    {
        var ruleEntity = await context.Rules
            .FirstAsync(x => x.RuleId == ruleId && !x.IsDeleted, stoppingToken);

        ruleEntity.RuleSetId = targetRuleSetId;
        ruleEntity.UpdatedAt = timeProvider.GetUtcNow();

        context.Entry(ruleEntity).Property(x => x.RuleSetId).IsModified = true;

        context.Rules.Update(ruleEntity);
    }

    public async Task<Exists> ExistsAsync(OrganizationId organizationId, RuleSetId ruleSetId, RuleId ruleId, IncludeDeleted includeDeleted, CancellationToken stoppingToken)
    {
        var exists = await context.Rules
            .AnyAsync(x => x.RuleId == ruleId &&
                           x.OrganizationId == organizationId &&
                           x.RuleSetId == ruleSetId &&
                           (includeDeleted || x.IsDeleted == false) &&
                           !x.RuleSet.IsDeleted, stoppingToken);

        return new Exists(exists);
    }

    public async Task<Exists> ExistsAsync(OrganizationId organizationId, RuleId ruleId, IncludeDeleted includeDeleted, CancellationToken stoppingToken)
    {
        var exists = await context.Rules
            .AnyAsync(x => x.RuleId == ruleId &&
                           x.OrganizationId == organizationId &&
                           (includeDeleted || x.IsDeleted == false), stoppingToken);

        return new Exists(exists);
    }

    public async Task<List<Rule>> ListByOrganizationAsync(OrganizationId organizationId, CancellationToken stoppingToken)
    {
        var ruleEntities = await context.Rules
            .Where(x => x.OrganizationId == organizationId &&
                        !x.IsDeleted &&
                        !x.RuleSet.IsDeleted)
            .ToListAsync(stoppingToken);

        return ruleEntities.ConvertAll(x => x.ToModel());
    }

    public async Task<Rule> GetAsync(RuleId ruleId, CancellationToken stoppingToken)
    {
        var ruleEntity = await context.Rules
            .FirstAsync(x => x.RuleId == ruleId &&
                             !x.IsDeleted &&
                             !x.RuleSet.IsDeleted, stoppingToken);

        return ruleEntity.ToModel();
    }

    public async Task<List<Rule>> ListByIdsAsync(IReadOnlyCollection<RuleId> ruleIds, CancellationToken stoppingToken)
    {
        if (ruleIds.Count == 0)
        {
            return [];
        }

        var ruleIdValues = ruleIds.Select(x => (Guid)x).Distinct().ToList();

        var ruleEntities = await context.Rules
            .Where(x => ruleIdValues.Contains(x.RuleId) && !x.RuleSet.IsDeleted)
            .ToListAsync(stoppingToken);

        return ruleEntities.ConvertAll(x => x.ToModel());
    }

    public async Task DeleteAsync(RuleId ruleId, CancellationToken stoppingToken)
    {
        var ruleEntity = await context.Rules
            .FirstAsync(x => x.RuleId == ruleId && !x.IsDeleted, stoppingToken);

        ruleEntity.IsDeleted = true;
        ruleEntity.DeletedAt = timeProvider.GetUtcNow();

        context.Rules.Update(ruleEntity);
    }

    public async Task RecoverAsync(RuleId ruleId, CancellationToken stoppingToken)
    {
        var ruleEntity = await context.Rules
            .FirstAsync(x => x.RuleId == ruleId, stoppingToken);

        ruleEntity.IsDeleted = false;
        ruleEntity.DeletedAt = null;

        context.Rules.Update(ruleEntity);
    }

    protected override Task<List<Rule>> MapPageAsync(List<RuleEntity> items, ListRuleRequest request, CancellationToken stoppingToken)
    {
        return Task.FromResult(items.ConvertAll(x => x.ToModel()));
    }

    protected override IOrderByExpression<RuleEntity> OrderByExpression()
    {
        return new OrderByExpression<RuleEntity, DateTimeOffset>(x => x.UpdatedAt);
    }

    protected override IQueryable<RuleEntity> FilterByPagedRequest(ListRuleRequest listRuleRequest)
    {
        var queryable = context.Rules
            .Where(x => x.OrganizationId == listRuleRequest.OrganizationId &&
                        x.RuleSetId == listRuleRequest.RuleSetId &&
                        !x.IsDeleted &&
                        !x.RuleSet.IsDeleted)
            .ApplyPermissionFilter(listRuleRequest.PermissionFilter);

        if (listRuleRequest.FriendlyName.HasValue)
        {
            queryable = queryable
                .Where(x => EF.Functions.ILike(x.FriendlyName, $"%{listRuleRequest.FriendlyName}%"));
        }

        return queryable;
    }

    protected override IQueryable<RuleEntity> FilterByDateRange(IQueryable<RuleEntity> queryable, DatedRequest datedRequest)
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