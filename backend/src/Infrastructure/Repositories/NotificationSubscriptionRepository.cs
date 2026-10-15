using Core.Interfaces.Repositories;
using Core.Models.Common;
using Core.Models.NotificationSubscriptions;
using Core.ValueObjects;
using Infrastructure.Entities;
using Infrastructure.Expressions;
using Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class NotificationSubscriptionRepository(
    TransactionMonitoringDbContext context,
    TimeProvider timeProvider) : PagedRepository<ListNotificationSubscriptionRequest, NotificationSubscriptionEntity, NotificationSubscription>, INotificationSubscriptionRepository
{
    public async Task AddAsync(NotificationSubscription notificationSubscription, CancellationToken stoppingToken)
    {
        var notificationSubscriptionEntity = notificationSubscription.ToEntity();
        await context.NotificationSubscriptions.AddAsync(notificationSubscriptionEntity, stoppingToken);
    }

    public async Task UpdateAsync(UpdateNotificationSubscriptionRequest updateNotificationSubscriptionRequest, CancellationToken stoppingToken)
    {
        var notificationSubscriptionEntity = await context.NotificationSubscriptions
            .FirstAsync(x => x.NotificationSubscriptionId == updateNotificationSubscriptionRequest.NotificationSubscriptionId && !x.IsDeleted, stoppingToken);

        notificationSubscriptionEntity.Destination = updateNotificationSubscriptionRequest.Destination?.ToString() ?? notificationSubscriptionEntity.Destination;
        notificationSubscriptionEntity.NotifyOnBreach = updateNotificationSubscriptionRequest.NotifyOnBreach ?? notificationSubscriptionEntity.NotifyOnBreach;
        notificationSubscriptionEntity.IsEnabled = updateNotificationSubscriptionRequest.IsEnabled ?? notificationSubscriptionEntity.IsEnabled;

        if (updateNotificationSubscriptionRequest.MinimumSeverity.HasValue)
        {
            notificationSubscriptionEntity.MinimumSeverityId = (int)updateNotificationSubscriptionRequest.MinimumSeverity.Value;
        }

        if (updateNotificationSubscriptionRequest.Locale.IsSet)
        {
            notificationSubscriptionEntity.Locale = updateNotificationSubscriptionRequest.Locale.Value?.ToString();
        }

        notificationSubscriptionEntity.UpdatedAt = timeProvider.GetUtcNow();

        context.NotificationSubscriptions.Update(notificationSubscriptionEntity);
    }

    public async Task<Exists> ExistsAsync(OrganizationId organizationId, NotificationSubscriptionId notificationSubscriptionId, IncludeDeleted includeDeleted, CancellationToken stoppingToken)
    {
        var exists = await context.NotificationSubscriptions
            .AnyAsync(x => x.NotificationSubscriptionId == notificationSubscriptionId &&
                           x.OrganizationId == organizationId &&
                           (includeDeleted || x.IsDeleted == false), stoppingToken);

        return new Exists(exists);
    }

    public async Task<NotificationSubscription> GetAsync(NotificationSubscriptionId notificationSubscriptionId, CancellationToken stoppingToken)
    {
        var notificationSubscriptionEntity = await context.NotificationSubscriptions
            .FirstAsync(x => x.NotificationSubscriptionId == notificationSubscriptionId && !x.IsDeleted, stoppingToken);

        return notificationSubscriptionEntity.ToModel();
    }

    public async Task DeleteAsync(NotificationSubscriptionId notificationSubscriptionId, CancellationToken stoppingToken)
    {
        var notificationSubscriptionEntity = await context.NotificationSubscriptions
            .FirstAsync(x => x.NotificationSubscriptionId == notificationSubscriptionId, stoppingToken);

        notificationSubscriptionEntity.IsDeleted = true;
        notificationSubscriptionEntity.DeletedAt = timeProvider.GetUtcNow();

        context.NotificationSubscriptions.Update(notificationSubscriptionEntity);
    }

    public async Task<List<NotificationSubscription>> ListEnabledAsync(OrganizationId organizationId, CancellationToken stoppingToken)
    {
        var notificationSubscriptionEntities = await context.NotificationSubscriptions
            .Where(x => x.OrganizationId == organizationId && x.IsEnabled && !x.IsDeleted)
            .ToListAsync(stoppingToken);

        return notificationSubscriptionEntities.ConvertAll(x => x.ToModel());
    }

    protected override Task<List<NotificationSubscription>> MapPageAsync(List<NotificationSubscriptionEntity> items, ListNotificationSubscriptionRequest request, CancellationToken stoppingToken)
    {
        return Task.FromResult(items.ConvertAll(x => x.ToModel()));
    }

    protected override IOrderByExpression<NotificationSubscriptionEntity> OrderByExpression()
    {
        return new OrderByExpression<NotificationSubscriptionEntity, DateTimeOffset>(x => x.UpdatedAt);
    }

    protected override IQueryable<NotificationSubscriptionEntity> FilterByPagedRequest(ListNotificationSubscriptionRequest listNotificationSubscriptionRequest)
    {
        var queryable = context.NotificationSubscriptions
            .Where(x => x.OrganizationId == listNotificationSubscriptionRequest.OrganizationId && !x.IsDeleted);

        if (listNotificationSubscriptionRequest.Channel.HasValue)
        {
            queryable = queryable
                .Where(x => x.ChannelId == (int)listNotificationSubscriptionRequest.Channel.Value);
        }

        if (listNotificationSubscriptionRequest.IsEnabled.HasValue)
        {
            queryable = queryable
                .Where(x => x.IsEnabled == listNotificationSubscriptionRequest.IsEnabled.Value);
        }

        return queryable;
    }

    protected override IQueryable<NotificationSubscriptionEntity> FilterByDateRange(IQueryable<NotificationSubscriptionEntity> queryable, DatedRequest datedRequest)
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
