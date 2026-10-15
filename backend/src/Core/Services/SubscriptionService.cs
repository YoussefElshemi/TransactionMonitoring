using Core.Exceptions;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Mappers;
using Core.Models.Common;
using Core.Models.Subscriptions;
using Core.ValueObjects;

namespace Core.Services;

public class SubscriptionService(
    ISubscriptionRepository subscriptionRepository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : ISubscriptionService
{
    public async Task<Subscription> CreateAsync(CreateSubscriptionRequest createSubscriptionRequest, CancellationToken stoppingToken)
    {
        var subscription = createSubscriptionRequest.ToModel(timeProvider);
        var latest = await subscriptionRepository.GetLatestAsync(createSubscriptionRequest.OrganizationId, stoppingToken);

        if (latest is not null)
        {
            if (subscription.EffectiveFrom <= (DateTimeOffset)latest.EffectiveFrom)
            {
                throw new SubscriptionTimelineException(
                    $"The organization's most recent subscription starts at {latest.EffectiveFrom}. A new one must start after that; cancel the scheduled change instead of backdating over it.");
            }

            if (latest.EffectiveTo is null)
            {
                await subscriptionRepository.CloseAsync(
                    latest.SubscriptionId, new EffectiveTo(subscription.EffectiveFrom), stoppingToken);
            }
        }

        await subscriptionRepository.AddAsync(subscription, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);

        return await GetAsync(subscription.SubscriptionId, stoppingToken);
    }

    public Task<Subscription> GetAsync(SubscriptionId subscriptionId, CancellationToken stoppingToken)
    {
        return subscriptionRepository.GetAsync(subscriptionId, stoppingToken);
    }

    public Task<Exists> ExistsAsync(OrganizationId organizationId, SubscriptionId subscriptionId, CancellationToken stoppingToken)
    {
        return subscriptionRepository.ExistsAsync(organizationId, subscriptionId, stoppingToken);
    }

    public Task<Subscription?> GetInEffectAsync(OrganizationId organizationId, DateTimeOffset asOf, CancellationToken stoppingToken)
    {
        return subscriptionRepository.GetInEffectAsync(organizationId, asOf, stoppingToken);
    }

    public Task<PagedResponse<Subscription>> ListAsync(ListSubscriptionRequest listSubscriptionRequest, CancellationToken stoppingToken)
    {
        return subscriptionRepository.ListAsync(listSubscriptionRequest, stoppingToken);
    }

    public Task<TotalCount> GetCountAsync(ListSubscriptionRequest listSubscriptionRequest, CancellationToken stoppingToken)
    {
        return subscriptionRepository.GetCountAsync(listSubscriptionRequest, stoppingToken);
    }

    public async Task DeleteAsync(SubscriptionId subscriptionId, CancellationToken stoppingToken)
    {
        var subscription = await GetAsync(subscriptionId, stoppingToken);
        var now = timeProvider.GetUtcNow();

        if (subscription.EffectiveFrom <= now)
        {
            throw new SubscriptionTimelineException(
                "The subscription has already taken effect. Schedule a move to another plan rather than cancelling this one.");
        }

        await subscriptionRepository.DeleteAsync(subscriptionId, stoppingToken);

        var predecessor = await subscriptionRepository.GetInEffectAsync(subscription.OrganizationId, now, stoppingToken);

        if (predecessor is not null && predecessor.EffectiveTo is not null)
        {
            await subscriptionRepository.ReopenAsync(predecessor.SubscriptionId, stoppingToken);
        }

        await unitOfWork.SaveChangesAsync(stoppingToken);
    }
}
