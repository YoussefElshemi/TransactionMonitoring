using Core.Interfaces.Repositories;
using Core.Models.Common;
using Core.Models.Subscriptions;
using Core.ValueObjects;
using Infrastructure.Entities;
using Infrastructure.Expressions;
using Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class SubscriptionRepository(
    TransactionMonitoringDbContext context,
    TimeProvider timeProvider) : PagedRepository<ListSubscriptionRequest, SubscriptionEntity, Subscription>, ISubscriptionRepository
{
    public async Task AddAsync(Subscription subscription, CancellationToken stoppingToken)
    {
        var subscriptionEntity = subscription.ToEntity();
        await context.Subscriptions.AddAsync(subscriptionEntity, stoppingToken);
    }

    public async Task<Subscription> GetAsync(SubscriptionId subscriptionId, CancellationToken stoppingToken)
    {
        var subscriptionEntity = await context.Subscriptions
            .FirstAsync(x => x.SubscriptionId == subscriptionId && !x.IsDeleted, stoppingToken);

        return subscriptionEntity.ToModel();
    }

    public async Task<Exists> ExistsAsync(OrganizationId organizationId, SubscriptionId subscriptionId, CancellationToken stoppingToken)
    {
        var exists = await context.Subscriptions
            .AnyAsync(x => x.OrganizationId == organizationId &&
                           x.SubscriptionId == subscriptionId &&
                           !x.IsDeleted, stoppingToken);

        return new Exists(exists);
    }

    public async Task<Subscription?> GetInEffectAsync(OrganizationId organizationId, DateTimeOffset asOf, CancellationToken stoppingToken)
    {
        var subscriptionEntity = await context.Subscriptions
            .Where(x => x.OrganizationId == organizationId &&
                        !x.IsDeleted &&
                        x.EffectiveFrom <= asOf &&
                        (x.EffectiveTo == null || x.EffectiveTo > asOf))
            .OrderByDescending(x => x.EffectiveFrom)
            .FirstOrDefaultAsync(stoppingToken);

        return subscriptionEntity?.ToModel();
    }

    public async Task<Subscription?> GetOpenAsync(OrganizationId organizationId, CancellationToken stoppingToken)
    {
        var subscriptionEntity = await context.Subscriptions
            .FirstOrDefaultAsync(x => x.OrganizationId == organizationId &&
                                      !x.IsDeleted &&
                                      x.EffectiveTo == null, stoppingToken);

        return subscriptionEntity?.ToModel();
    }

    public async Task<Subscription?> GetLatestAsync(OrganizationId organizationId, CancellationToken stoppingToken)
    {
        var subscriptionEntity = await context.Subscriptions
            .Where(x => x.OrganizationId == organizationId && !x.IsDeleted)
            .OrderByDescending(x => x.EffectiveFrom)
            .FirstOrDefaultAsync(stoppingToken);

        return subscriptionEntity?.ToModel();
    }

    // Load the row alone: the version auto-includes its plan, and two stretches on one plan would attach it twice.
    public async Task CloseAsync(SubscriptionId subscriptionId, EffectiveTo effectiveTo, CancellationToken stoppingToken)
    {
        var subscriptionEntity = await context.Subscriptions
            .IgnoreAutoIncludes()
            .FirstAsync(x => x.SubscriptionId == subscriptionId && !x.IsDeleted, stoppingToken);

        subscriptionEntity.EffectiveTo = effectiveTo;
        subscriptionEntity.UpdatedAt = timeProvider.GetUtcNow();

        context.Subscriptions.Update(subscriptionEntity);
    }

    public async Task ReopenAsync(SubscriptionId subscriptionId, CancellationToken stoppingToken)
    {
        var subscriptionEntity = await context.Subscriptions
            .IgnoreAutoIncludes()
            .FirstAsync(x => x.SubscriptionId == subscriptionId && !x.IsDeleted, stoppingToken);

        subscriptionEntity.EffectiveTo = null;
        subscriptionEntity.UpdatedAt = timeProvider.GetUtcNow();

        context.Subscriptions.Update(subscriptionEntity);
    }

    public async Task DeleteAsync(SubscriptionId subscriptionId, CancellationToken stoppingToken)
    {
        var subscriptionEntity = await context.Subscriptions
            .IgnoreAutoIncludes()
            .FirstAsync(x => x.SubscriptionId == subscriptionId && !x.IsDeleted, stoppingToken);

        subscriptionEntity.IsDeleted = true;
        subscriptionEntity.DeletedAt = timeProvider.GetUtcNow();
        subscriptionEntity.UpdatedAt = timeProvider.GetUtcNow();

        context.Subscriptions.Update(subscriptionEntity);
    }

    protected override Task<List<Subscription>> MapPageAsync(List<SubscriptionEntity> items, ListSubscriptionRequest request, CancellationToken stoppingToken)
    {
        return Task.FromResult(items.ConvertAll(x => x.ToModel()));
    }

    protected override IOrderByExpression<SubscriptionEntity> OrderByExpression()
    {
        return new OrderByExpression<SubscriptionEntity, DateTimeOffset>(x => x.EffectiveFrom);
    }

    protected override IQueryable<SubscriptionEntity> FilterByPagedRequest(ListSubscriptionRequest listSubscriptionRequest)
    {
        var queryable = context.Subscriptions
            .Where(x => x.OrganizationId == listSubscriptionRequest.OrganizationId && !x.IsDeleted);

        if (!listSubscriptionRequest.IncludeSuperseded)
        {
            var now = timeProvider.GetUtcNow();
            queryable = queryable.Where(x => x.EffectiveTo == null || x.EffectiveTo > now);
        }

        return queryable;
    }

    protected override IQueryable<SubscriptionEntity> FilterByDateRange(IQueryable<SubscriptionEntity> queryable, DatedRequest datedRequest)
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
