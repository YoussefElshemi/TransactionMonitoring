using Core.Models.NotificationSubscriptions;
using Core.ValueObjects;

namespace Core.Interfaces.Services;

public interface INotificationSubscriptionService : IPagedService<ListNotificationSubscriptionRequest, NotificationSubscription>
{
    Task<NotificationSubscription> CreateAsync(CreateNotificationSubscriptionRequest createNotificationSubscriptionRequest, CancellationToken stoppingToken);
    Task UpdateAsync(UpdateNotificationSubscriptionRequest updateNotificationSubscriptionRequest, CancellationToken stoppingToken);
    Task<Exists> ExistsAsync(OrganizationId organizationId, NotificationSubscriptionId notificationSubscriptionId, IncludeDeleted includeDeleted, CancellationToken stoppingToken);
    Task<NotificationSubscription> GetAsync(NotificationSubscriptionId notificationSubscriptionId, CancellationToken stoppingToken);
    Task DeleteAsync(NotificationSubscriptionId notificationSubscriptionId, CancellationToken stoppingToken);
    Task<List<NotificationSubscription>> ListEnabledAsync(OrganizationId organizationId, CancellationToken stoppingToken);
}
