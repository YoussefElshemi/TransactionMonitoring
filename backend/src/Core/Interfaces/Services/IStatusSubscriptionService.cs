using Core.Models.Status;

namespace Core.Interfaces.Services;

public interface IStatusSubscriptionService
{
    Task<IssuedStatusSubscription> SubscribeAsync(CreateStatusSubscriptionRequest createStatusSubscriptionRequest, CancellationToken stoppingToken);

    Task<StatusSubscription> ConfirmAsync(ConfirmStatusSubscriptionRequest confirmStatusSubscriptionRequest, CancellationToken stoppingToken);
    Task UnsubscribeAsync(UnsubscribeStatusSubscriptionRequest unsubscribeStatusSubscriptionRequest, CancellationToken stoppingToken);
    Task<List<StatusSubscription>> ListDeliverableAsync(CancellationToken stoppingToken);
}
