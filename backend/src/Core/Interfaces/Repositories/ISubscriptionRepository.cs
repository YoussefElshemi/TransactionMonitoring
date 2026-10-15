using Core.Models.Subscriptions;
using Core.ValueObjects;

namespace Core.Interfaces.Repositories;

public interface ISubscriptionRepository : IPagedRepository<ListSubscriptionRequest, Subscription>
{
    Task AddAsync(Subscription subscription, CancellationToken stoppingToken);
    Task<Subscription> GetAsync(SubscriptionId subscriptionId, CancellationToken stoppingToken);
    Task<Exists> ExistsAsync(OrganizationId organizationId, SubscriptionId subscriptionId, CancellationToken stoppingToken);

    Task<Subscription?> GetInEffectAsync(OrganizationId organizationId, DateTimeOffset asOf, CancellationToken stoppingToken);

    Task<Subscription?> GetOpenAsync(OrganizationId organizationId, CancellationToken stoppingToken);
    Task<Subscription?> GetLatestAsync(OrganizationId organizationId, CancellationToken stoppingToken);
    Task CloseAsync(SubscriptionId subscriptionId, EffectiveTo effectiveTo, CancellationToken stoppingToken);
    Task ReopenAsync(SubscriptionId subscriptionId, CancellationToken stoppingToken);
    Task DeleteAsync(SubscriptionId subscriptionId, CancellationToken stoppingToken);
}
