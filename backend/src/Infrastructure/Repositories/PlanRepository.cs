using Core.Interfaces.Repositories;
using Core.Models.Common;
using Core.Models.Plans;
using Core.ValueObjects;
using Infrastructure.Entities;
using Infrastructure.Expressions;
using Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class PlanRepository(
    TransactionMonitoringDbContext context,
    TimeProvider timeProvider) : PagedRepository<ListPlanRequest, PlanEntity, Plan>, IPlanRepository
{
    public async Task AddAsync(Plan plan, CancellationToken stoppingToken)
    {
        var planEntity = plan.ToEntity();
        await context.Plans.AddAsync(planEntity, stoppingToken);
    }

    public async Task<Plan> GetAsync(PlanId planId, CancellationToken stoppingToken)
    {
        var planEntity = await context.Plans
            .Include(x => x.PlanVersions)
            .FirstAsync(x => x.PlanId == planId && !x.IsDeleted, stoppingToken);

        return planEntity.ToModel();
    }

    public async Task<Exists> ExistsAsync(PlanId planId, CancellationToken stoppingToken)
    {
        var exists = await context.Plans
            .AnyAsync(x => x.PlanId == planId && !x.IsDeleted, stoppingToken);

        return new Exists(exists);
    }

    public async Task<Exists> ExistsByNameAsync(PlanName name, OrganizationId? organizationId, CancellationToken stoppingToken)
    {
        Guid? owner = organizationId.HasValue ? organizationId.Value : null;

        var exists = await context.Plans
            .AnyAsync(x => x.Name == (string)name && x.OrganizationId == owner && !x.IsDeleted, stoppingToken);

        return new Exists(exists);
    }

    public async Task<Exists> HasSubscriptionsAsync(PlanId planId, CancellationToken stoppingToken)
    {
        var exists = await context.Subscriptions
            .AnyAsync(x => x.PlanVersion.PlanId == planId, stoppingToken);

        return new Exists(exists);
    }

    public async Task UpdateAsync(UpdatePlanRequest updatePlanRequest, CancellationToken stoppingToken)
    {
        var planEntity = await context.Plans
            .FirstAsync(x => x.PlanId == updatePlanRequest.PlanId && !x.IsDeleted, stoppingToken);

        planEntity.Name = updatePlanRequest.Name ?? planEntity.Name;
        planEntity.IsEnabled = updatePlanRequest.IsEnabled ?? planEntity.IsEnabled;

        if (updatePlanRequest.Description.IsSet)
        {
            planEntity.Description = updatePlanRequest.Description.Value?.ToString();
        }

        planEntity.UpdatedAt = timeProvider.GetUtcNow();

        context.Plans.Update(planEntity);
    }

    public async Task SetCurrentVersionAsync(PlanId planId, VersionNumber versionNumber, CancellationToken stoppingToken)
    {
        var planEntity = await context.Plans
            .FirstAsync(x => x.PlanId == planId && !x.IsDeleted, stoppingToken);

        planEntity.CurrentVersionNumber = versionNumber;
        planEntity.UpdatedAt = timeProvider.GetUtcNow();

        context.Plans.Update(planEntity);
    }

    public async Task DeleteAsync(PlanId planId, CancellationToken stoppingToken)
    {
        var planEntity = await context.Plans
            .FirstAsync(x => x.PlanId == planId && !x.IsDeleted, stoppingToken);

        planEntity.IsDeleted = true;
        planEntity.DeletedAt = timeProvider.GetUtcNow();
        planEntity.UpdatedAt = timeProvider.GetUtcNow();

        context.Plans.Update(planEntity);
    }

    protected override Task<List<Plan>> MapPageAsync(List<PlanEntity> items, ListPlanRequest request, CancellationToken stoppingToken)
    {
        return Task.FromResult(items.ConvertAll(x => x.ToModel()));
    }

    protected override IOrderByExpression<PlanEntity> OrderByExpression()
    {
        return new OrderByExpression<PlanEntity, string>(x => x.Name);
    }

    protected override IQueryable<PlanEntity> FilterByPagedRequest(ListPlanRequest listPlanRequest)
    {
        var queryable = context.Plans
            .Include(x => x.PlanVersions)
            .Where(x => !x.IsDeleted);

        if (!listPlanRequest.IncludeDisabled)
        {
            queryable = queryable.Where(x => x.IsEnabled);
        }

        if (listPlanRequest.OrganizationId.HasValue)
        {
            Guid organizationId = listPlanRequest.OrganizationId.Value;
            queryable = queryable.Where(x => x.OrganizationId == null || x.OrganizationId == organizationId);
        }

        if (listPlanRequest.Name.HasValue)
        {
            queryable = queryable
                .Where(x => EF.Functions.ILike(x.Name, $"%{listPlanRequest.Name}%"));
        }

        return queryable;
    }

    protected override IQueryable<PlanEntity> FilterByDateRange(IQueryable<PlanEntity> queryable, DatedRequest datedRequest)
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
