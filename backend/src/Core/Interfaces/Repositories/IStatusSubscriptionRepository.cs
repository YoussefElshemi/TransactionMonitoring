using Core.Models.Status;
using Core.ValueObjects;

namespace Core.Interfaces.Repositories;

public interface IStatusSubscriptionRepository
{
    Task AddAsync(StatusSubscription statusSubscription, Email normalizedEmail, Hash confirmationTokenHash, Hash unsubscribeTokenHash, CancellationToken stoppingToken);
    Task<StatusSubscription?> GetByEmailAsync(Email normalizedEmail, CancellationToken stoppingToken);
    Task<StatusSubscription?> GetAsync(StatusSubscriptionId statusSubscriptionId, CancellationToken stoppingToken);

    Task<Exists> MatchesConfirmationTokenAsync(StatusSubscriptionId statusSubscriptionId, Hash tokenHash, CancellationToken stoppingToken);
    Task<Exists> MatchesUnsubscribeTokenAsync(StatusSubscriptionId statusSubscriptionId, Hash tokenHash, CancellationToken stoppingToken);

    Task ReissueConfirmationAsync(StatusSubscriptionId statusSubscriptionId, Hash confirmationTokenHash, CancellationToken stoppingToken);
    Task ConfirmAsync(StatusSubscriptionId statusSubscriptionId, CancellationToken stoppingToken);
    Task UnsubscribeAsync(StatusSubscriptionId statusSubscriptionId, CancellationToken stoppingToken);

    Task<List<StatusSubscription>> ListDeliverableAsync(CancellationToken stoppingToken);
}
