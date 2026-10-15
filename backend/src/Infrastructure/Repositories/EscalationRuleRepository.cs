using Core.Interfaces.Repositories;
using Core.Models.Common;
using Core.Models.Escalations;
using Core.ValueObjects;
using Infrastructure.Entities;
using Infrastructure.Expressions;
using Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class EscalationRuleRepository(
    TransactionMonitoringDbContext context,
    TimeProvider timeProvider)
    : PagedRepository<ListEscalationRuleRequest, EscalationRuleEntity, EscalationRule>, IEscalationRuleRepository
{
    private const string LikeEscapeCharacter = "\\";

    public async Task AddAsync(EscalationRule escalationRule, CancellationToken stoppingToken)
    {
        var escalationRuleEntity = escalationRule.ToEntity();

        await context.EscalationRules.AddAsync(escalationRuleEntity, stoppingToken);
    }

    public async Task<EscalationRule> GetAsync(EscalationRuleId escalationRuleId, IncludeDeleted includeDeleted, CancellationToken stoppingToken)
    {
        var escalationRuleEntity = await context.EscalationRules
            .FirstAsync(x => x.EscalationRuleId == escalationRuleId && (includeDeleted || !x.IsDeleted), stoppingToken);

        return escalationRuleEntity.ToModel();
    }

    // Escaped: this is an equality test written as LIKE, and underscores would otherwise be wildcards.
    public async Task<EscalationRule?> FindByNameAsync(OrganizationId organizationId, EscalationRuleName name, CancellationToken stoppingToken)
    {
        var value = EscapeLikePattern(name.ToString());

        var escalationRuleEntity = await context.EscalationRules
            .FirstOrDefaultAsync(x => x.OrganizationId == organizationId &&
                                      EF.Functions.ILike(x.Name, value, LikeEscapeCharacter) &&
                                      !x.IsDeleted, stoppingToken);

        return escalationRuleEntity?.ToModel();
    }

    public async Task<Exists> ExistsAsync(OrganizationId organizationId, EscalationRuleId escalationRuleId, IncludeDeleted includeDeleted, CancellationToken stoppingToken)
    {
        var exists = await context.EscalationRules
            .AnyAsync(x => x.OrganizationId == organizationId &&
                           x.EscalationRuleId == escalationRuleId &&
                           (includeDeleted || x.IsDeleted == false), stoppingToken);

        return new Exists(exists);
    }

    public async Task<List<EscalationRule>> ListActiveAsync(OrganizationId organizationId, CancellationToken stoppingToken)
    {
        var escalationRuleEntities = await context.EscalationRules
            .Where(x => x.OrganizationId == organizationId && x.IsEnabled && !x.IsDeleted)
            .OrderBy(x => x.Priority)
            .ThenBy(x => x.CreatedAt)
            .ToListAsync(stoppingToken);

        return escalationRuleEntities.ConvertAll(x => x.ToModel());
    }

    public async Task<List<OrganizationId>> ListOrganizationsWithActiveRulesAsync(CancellationToken stoppingToken)
    {
        var organizationIds = await context.EscalationRules
            .Where(x => x.IsEnabled && !x.IsDeleted)
            .Select(x => x.OrganizationId)
            .Distinct()
            .ToListAsync(stoppingToken);

        return organizationIds.ConvertAll(x => new OrganizationId(x));
    }

    public async Task UpdateAsync(UpdateEscalationRuleRequest updateEscalationRuleRequest, CancellationToken stoppingToken)
    {
        var escalationRuleEntity = await context.EscalationRules
            .FirstAsync(x => x.EscalationRuleId == updateEscalationRuleRequest.EscalationRuleId && !x.IsDeleted, stoppingToken);

        escalationRuleEntity.Name = updateEscalationRuleRequest.Name?.ToString() ?? escalationRuleEntity.Name;
        escalationRuleEntity.Description = updateEscalationRuleRequest.Description?.ToString() ?? escalationRuleEntity.Description;
        escalationRuleEntity.Priority = updateEscalationRuleRequest.Priority ?? escalationRuleEntity.Priority;
        escalationRuleEntity.IsEnabled = updateEscalationRuleRequest.IsEnabled ?? escalationRuleEntity.IsEnabled;

        if (updateEscalationRuleRequest.ClearUnassignedFor)
        {
            escalationRuleEntity.UnassignedForMinutes = null;
        }
        else if (updateEscalationRuleRequest.UnassignedFor.HasValue)
        {
            escalationRuleEntity.UnassignedForMinutes = updateEscalationRuleRequest.UnassignedFor.Value;
        }

        if (updateEscalationRuleRequest.ClearHoldExpiryWithin)
        {
            escalationRuleEntity.HoldExpiryWithinMinutes = null;
        }
        else if (updateEscalationRuleRequest.HoldExpiryWithin.HasValue)
        {
            escalationRuleEntity.HoldExpiryWithinMinutes = updateEscalationRuleRequest.HoldExpiryWithin.Value;
        }

        if (updateEscalationRuleRequest.ClearMinimumSeverity)
        {
            escalationRuleEntity.MinimumSeverityId = null;
        }
        else if (updateEscalationRuleRequest.MinimumSeverity.HasValue)
        {
            escalationRuleEntity.MinimumSeverityId = (int)updateEscalationRuleRequest.MinimumSeverity.Value;
        }

        if (updateEscalationRuleRequest.ClearMatcher)
        {
            escalationRuleEntity.Matcher = null;
        }
        else if (updateEscalationRuleRequest.Matcher is not null)
        {
            escalationRuleEntity.Matcher = updateEscalationRuleRequest.Matcher.ToBase64String();
        }

        if (updateEscalationRuleRequest.ClearTargetQueueId)
        {
            escalationRuleEntity.TargetQueueId = null;
        }
        else if (updateEscalationRuleRequest.TargetQueueId.HasValue)
        {
            escalationRuleEntity.TargetQueueId = updateEscalationRuleRequest.TargetQueueId.Value;
        }

        if (updateEscalationRuleRequest.ClearRaiseSeverityTo)
        {
            escalationRuleEntity.RaiseSeverityToId = null;
        }
        else if (updateEscalationRuleRequest.RaiseSeverityTo.HasValue)
        {
            escalationRuleEntity.RaiseSeverityToId = (int)updateEscalationRuleRequest.RaiseSeverityTo.Value;
        }

        escalationRuleEntity.UpdatedAt = timeProvider.GetUtcNow();

        context.EscalationRules.Update(escalationRuleEntity);
    }

    public async Task DeleteAsync(EscalationRuleId escalationRuleId, CancellationToken stoppingToken)
    {
        var escalationRuleEntity = await context.EscalationRules
            .FirstAsync(x => x.EscalationRuleId == escalationRuleId, stoppingToken);

        escalationRuleEntity.IsDeleted = true;
        escalationRuleEntity.DeletedAt = timeProvider.GetUtcNow();

        context.EscalationRules.Update(escalationRuleEntity);
    }

    public async Task RecoverAsync(EscalationRuleId escalationRuleId, CancellationToken stoppingToken)
    {
        var escalationRuleEntity = await context.EscalationRules
            .FirstAsync(x => x.EscalationRuleId == escalationRuleId, stoppingToken);

        escalationRuleEntity.IsDeleted = false;
        escalationRuleEntity.DeletedAt = null;

        context.EscalationRules.Update(escalationRuleEntity);
    }

    protected override Task<List<EscalationRule>> MapPageAsync(List<EscalationRuleEntity> items, ListEscalationRuleRequest request, CancellationToken stoppingToken)
    {
        return Task.FromResult(items.ConvertAll(x => x.ToModel()));
    }

    protected override IOrderByExpression<EscalationRuleEntity> OrderByExpression()
    {
        return new OrderByExpression<EscalationRuleEntity, int>(x => x.Priority, true, x => x.CreatedAt);
    }

    protected override IQueryable<EscalationRuleEntity> FilterByPagedRequest(ListEscalationRuleRequest listEscalationRuleRequest)
    {
        var queryable = context.EscalationRules
            .Where(x => x.OrganizationId == listEscalationRuleRequest.OrganizationId && !x.IsDeleted);

        if (listEscalationRuleRequest.Name.HasValue)
        {
            queryable = queryable
                .Where(x => EF.Functions.ILike(x.Name, $"%{listEscalationRuleRequest.Name}%"));
        }

        if (listEscalationRuleRequest.IsEnabled.HasValue)
        {
            queryable = queryable
                .Where(x => x.IsEnabled == listEscalationRuleRequest.IsEnabled.Value);
        }

        return queryable;
    }

    protected override IQueryable<EscalationRuleEntity> FilterByDateRange(IQueryable<EscalationRuleEntity> queryable, DatedRequest datedRequest)
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

    private static string EscapeLikePattern(string value)
    {
        return value
            .Replace(LikeEscapeCharacter, LikeEscapeCharacter + LikeEscapeCharacter, StringComparison.Ordinal)
            .Replace("%", LikeEscapeCharacter + "%", StringComparison.Ordinal)
            .Replace("_", LikeEscapeCharacter + "_", StringComparison.Ordinal);
    }
}
