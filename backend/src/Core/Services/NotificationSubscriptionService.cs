using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Mappers;
using Core.Models.Common;
using Core.Models.NotificationSubscriptions;
using Core.ValueObjects;

namespace Core.Services;

public class NotificationSubscriptionService(
    INotificationSubscriptionRepository notificationSubscriptionRepository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IModeContext modeContext) : INotificationSubscriptionService
{
    public async Task<NotificationSubscription> CreateAsync(CreateNotificationSubscriptionRequest createNotificationSubscriptionRequest, CancellationToken stoppingToken)
    {
        var notificationSubscription = createNotificationSubscriptionRequest.ToModel(timeProvider, modeContext.CurrentOrLive);

        await notificationSubscriptionRepository.AddAsync(notificationSubscription, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);

        return await GetAsync(notificationSubscription.NotificationSubscriptionId, stoppingToken);
    }

    public async Task UpdateAsync(UpdateNotificationSubscriptionRequest updateNotificationSubscriptionRequest, CancellationToken stoppingToken)
    {
        await notificationSubscriptionRepository.UpdateAsync(updateNotificationSubscriptionRequest, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);
    }

    public Task<Exists> ExistsAsync(OrganizationId organizationId, NotificationSubscriptionId notificationSubscriptionId, IncludeDeleted includeDeleted, CancellationToken stoppingToken)
    {
        return notificationSubscriptionRepository.ExistsAsync(organizationId, notificationSubscriptionId, includeDeleted, stoppingToken);
    }

    public Task<PagedResponse<NotificationSubscription>> ListAsync(ListNotificationSubscriptionRequest listNotificationSubscriptionRequest, CancellationToken stoppingToken)
    {
        return notificationSubscriptionRepository.ListAsync(listNotificationSubscriptionRequest, stoppingToken);
    }

    public Task<NotificationSubscription> GetAsync(NotificationSubscriptionId notificationSubscriptionId, CancellationToken stoppingToken)
    {
        return notificationSubscriptionRepository.GetAsync(notificationSubscriptionId, stoppingToken);
    }

    public async Task DeleteAsync(NotificationSubscriptionId notificationSubscriptionId, CancellationToken stoppingToken)
    {
        await notificationSubscriptionRepository.DeleteAsync(notificationSubscriptionId, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);
    }

    public Task<List<NotificationSubscription>> ListEnabledAsync(OrganizationId organizationId, CancellationToken stoppingToken)
    {
        return notificationSubscriptionRepository.ListEnabledAsync(organizationId, stoppingToken);
    }

    public Task<TotalCount> GetCountAsync(ListNotificationSubscriptionRequest listNotificationSubscriptionRequest, CancellationToken stoppingToken)
    {
        return notificationSubscriptionRepository.GetCountAsync(listNotificationSubscriptionRequest, stoppingToken);
    }
}
