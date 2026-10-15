using Core.Enums;
using Core.Interfaces.Repositories;
using Core.Models.Common;
using Core.Models.RuleDeployments;
using Core.ValueObjects;
using Infrastructure.Entities;
using Infrastructure.Expressions;
using Infrastructure.Extensions;
using Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class RuleDeploymentRepository(
    TransactionMonitoringDbContext context,
    TimeProvider timeProvider) : PagedRepository<ListRuleDeploymentRequest, RuleDeploymentEntity, RuleDeployment>, IRuleDeploymentRepository
{
    public async Task AddAsync(RuleDeployment ruleDeployment, CancellationToken stoppingToken)
    {
        await context.RuleDeployments.AddAsync(ruleDeployment.ToEntity(), stoppingToken);

        if (ruleDeployment.Status == RuleDeploymentStatus.InProgress)
        {
            await SetRuleActiveDeploymentFlagAsync(ruleDeployment.RuleId, true, stoppingToken);
        }
    }

    public async Task<RuleDeployment> GetAsync(RuleDeploymentId ruleDeploymentId, CancellationToken stoppingToken)
    {
        var ruleDeploymentEntity = await context.RuleDeployments
            .FirstAsync(x => x.RuleDeploymentId == ruleDeploymentId, stoppingToken);

        return ruleDeploymentEntity.ToModel();
    }

    public async Task<RuleDeployment?> GetActiveByRuleIdAsync(RuleId ruleId, CancellationToken stoppingToken)
    {
        var ruleDeploymentEntity = await context.RuleDeployments
            .FirstOrDefaultAsync(x => x.RuleId == ruleId &&
                                      x.StatusId == (int)RuleDeploymentStatus.InProgress, stoppingToken);

        return ruleDeploymentEntity?.ToModel();
    }

    public async Task<List<RuleDeployment>> ListByIdsAsync(IReadOnlyCollection<RuleDeploymentId> ruleDeploymentIds, CancellationToken stoppingToken)
    {
        if (ruleDeploymentIds.Count == 0)
        {
            return [];
        }

        var ruleDeploymentIdValues = ruleDeploymentIds.Select(x => (Guid)x).Distinct().ToList();

        var ruleDeploymentEntities = await context.RuleDeployments
            .Where(x => ruleDeploymentIdValues.Contains(x.RuleDeploymentId))
            .ToListAsync(stoppingToken);

        return ruleDeploymentEntities.ConvertAll(x => x.ToModel());
    }

    public async Task<Exists> ExistsActiveAsync(RuleId ruleId, CancellationToken stoppingToken)
    {
        var exists = await context.RuleDeployments
            .AnyAsync(x => x.RuleId == ruleId &&
                           x.StatusId == (int)RuleDeploymentStatus.InProgress, stoppingToken);

        return new Exists(exists);
    }

    public async Task<Exists> ExistsPendingApprovalAsync(RuleId ruleId, CancellationToken stoppingToken)
    {
        var exists = await context.RuleDeployments
            .AnyAsync(x => x.RuleId == ruleId &&
                           x.StatusId == (int)RuleDeploymentStatus.PendingApproval, stoppingToken);

        return new Exists(exists);
    }

    public async Task<Exists> ExistsInProgressAsync(OrganizationId organizationId, RuleDeploymentId ruleDeploymentId, CancellationToken stoppingToken)
    {
        var exists = await context.RuleDeployments
            .AnyAsync(x => x.RuleDeploymentId == ruleDeploymentId &&
                           x.OrganizationId == organizationId &&
                           x.StatusId == (int)RuleDeploymentStatus.InProgress, stoppingToken);

        return new Exists(exists);
    }

    public async Task UpdateAsync(UpdateRuleDeploymentRequest request, CancellationToken stoppingToken)
    {
        var ruleDeploymentEntity = await context.RuleDeployments
            .ForUpdate()
            .FirstAsync(x => x.RuleDeploymentId == request.RuleDeploymentId, stoppingToken);

        ruleDeploymentEntity.StatusId = (int)request.Status;
        ruleDeploymentEntity.CurrentPercentage = request.CurrentPercentage;
        ruleDeploymentEntity.CompletedAt = request.CompletedAt;
        ruleDeploymentEntity.UpdatedAt = timeProvider.GetUtcNow();

        await SetRuleActiveDeploymentFlagAsync(
            new RuleId(ruleDeploymentEntity.RuleId), request.Status == RuleDeploymentStatus.InProgress, stoppingToken);
    }

    private async Task SetRuleActiveDeploymentFlagAsync(RuleId ruleId, bool hasActiveDeployment, CancellationToken stoppingToken)
    {
        var ruleEntity = await context.Rules
            .ForUpdate()
            .FirstOrDefaultAsync(x => x.RuleId == ruleId, stoppingToken);

        if (ruleEntity is null)
        {
            return;
        }

        ruleEntity.HasActiveDeployment = hasActiveDeployment;
    }

    protected override Task<List<RuleDeployment>> MapPageAsync(List<RuleDeploymentEntity> items, ListRuleDeploymentRequest request, CancellationToken stoppingToken)
    {
        return Task.FromResult(items.ConvertAll(x => x.ToModel()));
    }

    protected override IOrderByExpression<RuleDeploymentEntity> OrderByExpression()
    {
        return new OrderByExpression<RuleDeploymentEntity, DateTimeOffset>(x => x.CreatedAt);
    }

    protected override IQueryable<RuleDeploymentEntity> FilterByPagedRequest(ListRuleDeploymentRequest request)
    {
        var queryable = context.RuleDeployments
            .Where(x => x.OrganizationId == request.OrganizationId);

        if (request.RuleId.HasValue)
        {
            queryable = queryable.Where(x => x.RuleId == request.RuleId.Value);
        }

        return queryable;
    }

    protected override IQueryable<RuleDeploymentEntity> FilterByDateRange(IQueryable<RuleDeploymentEntity> queryable, DatedRequest datedRequest)
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
