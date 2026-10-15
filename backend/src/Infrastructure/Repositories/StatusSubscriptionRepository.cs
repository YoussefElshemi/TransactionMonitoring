using Core.Interfaces.Repositories;
using Core.Models.Status;
using Core.ValueObjects;
using Infrastructure.Entities;
using Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class StatusSubscriptionRepository(
    TransactionMonitoringDbContext context,
    TimeProvider timeProvider) : IStatusSubscriptionRepository
{
    public async Task AddAsync(StatusSubscription statusSubscription, Email normalizedEmail, Hash confirmationTokenHash, Hash unsubscribeTokenHash, CancellationToken stoppingToken)
    {
        var statusSubscriptionEntity = new StatusSubscriptionEntity
        {
            StatusSubscriptionId = statusSubscription.StatusSubscriptionId,
            Email = statusSubscription.Email,
            NormalizedEmail = normalizedEmail,
            ConfirmationTokenHash = confirmationTokenHash,
            UnsubscribeTokenHash = unsubscribeTokenHash,
            ConfirmedAt = null,
            UnsubscribedAt = null,
            CreatedAt = statusSubscription.CreatedAt,
            UpdatedAt = statusSubscription.UpdatedAt
        };

        await context.StatusSubscriptions.AddAsync(statusSubscriptionEntity, stoppingToken);
    }

    public async Task<StatusSubscription?> GetByEmailAsync(Email normalizedEmail, CancellationToken stoppingToken)
    {
        var statusSubscriptionEntity = await context.StatusSubscriptions
            .FirstOrDefaultAsync(x => x.NormalizedEmail == (string)normalizedEmail, stoppingToken);

        return statusSubscriptionEntity?.ToModel();
    }

    public async Task<StatusSubscription?> GetAsync(StatusSubscriptionId statusSubscriptionId, CancellationToken stoppingToken)
    {
        var statusSubscriptionEntity = await context.StatusSubscriptions
            .FirstOrDefaultAsync(x => x.StatusSubscriptionId == statusSubscriptionId, stoppingToken);

        return statusSubscriptionEntity?.ToModel();
    }

    public async Task<Exists> MatchesConfirmationTokenAsync(StatusSubscriptionId statusSubscriptionId, Hash tokenHash, CancellationToken stoppingToken)
    {
        var exists = await context.StatusSubscriptions
            .AnyAsync(x => x.StatusSubscriptionId == statusSubscriptionId &&
                           x.ConfirmationTokenHash == (string)tokenHash, stoppingToken);

        return new Exists(exists);
    }

    public async Task<Exists> MatchesUnsubscribeTokenAsync(StatusSubscriptionId statusSubscriptionId, Hash tokenHash, CancellationToken stoppingToken)
    {
        var exists = await context.StatusSubscriptions
            .AnyAsync(x => x.StatusSubscriptionId == statusSubscriptionId &&
                           x.UnsubscribeTokenHash == (string)tokenHash, stoppingToken);

        return new Exists(exists);
    }

    public async Task ReissueConfirmationAsync(StatusSubscriptionId statusSubscriptionId, Hash confirmationTokenHash, CancellationToken stoppingToken)
    {
        var statusSubscriptionEntity = await context.StatusSubscriptions
            .FirstAsync(x => x.StatusSubscriptionId == statusSubscriptionId, stoppingToken);

        statusSubscriptionEntity.ConfirmationTokenHash = confirmationTokenHash;
        statusSubscriptionEntity.UnsubscribedAt = null;
        statusSubscriptionEntity.UpdatedAt = timeProvider.GetUtcNow();

        context.StatusSubscriptions.Update(statusSubscriptionEntity);
    }

    public async Task ConfirmAsync(StatusSubscriptionId statusSubscriptionId, CancellationToken stoppingToken)
    {
        var statusSubscriptionEntity = await context.StatusSubscriptions
            .FirstAsync(x => x.StatusSubscriptionId == statusSubscriptionId, stoppingToken);

        var now = timeProvider.GetUtcNow();

        statusSubscriptionEntity.ConfirmedAt = now;
        statusSubscriptionEntity.UnsubscribedAt = null;
        statusSubscriptionEntity.UpdatedAt = now;

        context.StatusSubscriptions.Update(statusSubscriptionEntity);
    }

    public async Task UnsubscribeAsync(StatusSubscriptionId statusSubscriptionId, CancellationToken stoppingToken)
    {
        var statusSubscriptionEntity = await context.StatusSubscriptions
            .FirstAsync(x => x.StatusSubscriptionId == statusSubscriptionId, stoppingToken);

        var now = timeProvider.GetUtcNow();

        statusSubscriptionEntity.UnsubscribedAt = now;
        statusSubscriptionEntity.UpdatedAt = now;

        context.StatusSubscriptions.Update(statusSubscriptionEntity);
    }

    public async Task<List<StatusSubscription>> ListDeliverableAsync(CancellationToken stoppingToken)
    {
        var statusSubscriptionEntities = await context.StatusSubscriptions
            .Where(x => x.ConfirmedAt != null && x.UnsubscribedAt == null)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(stoppingToken);

        return statusSubscriptionEntities.ConvertAll(x => x.ToModel());
    }
}
