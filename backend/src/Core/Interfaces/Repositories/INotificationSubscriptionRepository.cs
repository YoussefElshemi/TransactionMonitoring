using Core.Models.NotificationSubscriptions;
using Core.ValueObjects;

namespace Core.Interfaces.Repositories;

public interface INotificationSubscriptionRepository : IPagedRepository<ListNotificationSubscriptionRequest, NotificationSubscription>
{
    Task AddAsync(NotificationSubscription notificationSubscription, CancellationToken stoppingToken);
    Task UpdateAsync(UpdateNotificationSubscriptionRequest updateNotificationSubscriptionRequest, CancellationToken stoppingToken);
    Task<Exists> ExistsAsync(OrganizationId organizationId, NotificationSubscriptionId notificationSubscriptionId, IncludeDeleted includeDeleted, CancellationToken stoppingToken);
    Task<NotificationSubscription> GetAsync(NotificationSubscriptionId notificationSubscriptionId, CancellationToken stoppingToken);
    Task DeleteAsync(NotificationSubscriptionId notificationSubscriptionId, CancellationToken stoppingToken);

    Task<List<NotificationSubscription>> ListEnabledAsync(OrganizationId organizationId, CancellationToken stoppingToken);
}
