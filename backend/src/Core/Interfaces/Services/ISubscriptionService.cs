using Core.Models.Subscriptions;
using Core.ValueObjects;

namespace Core.Interfaces.Services;

public interface ISubscriptionService : IPagedService<ListSubscriptionRequest, Subscription>
{
    Task<Subscription> CreateAsync(CreateSubscriptionRequest createSubscriptionRequest, CancellationToken stoppingToken);
    Task<Subscription> GetAsync(SubscriptionId subscriptionId, CancellationToken stoppingToken);
    Task<Exists> ExistsAsync(OrganizationId organizationId, SubscriptionId subscriptionId, CancellationToken stoppingToken);
    Task<Subscription?> GetInEffectAsync(OrganizationId organizationId, DateTimeOffset asOf, CancellationToken stoppingToken);
    Task DeleteAsync(SubscriptionId subscriptionId, CancellationToken stoppingToken);
}
