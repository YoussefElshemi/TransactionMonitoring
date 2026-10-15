using Core.Exceptions;
using Core.Helpers;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Models.Status;
using Core.ValueObjects;

namespace Core.Services;

public class StatusSubscriptionService(
    IStatusSubscriptionRepository statusSubscriptionRepository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IStatusSubscriptionService
{
    public async Task<IssuedStatusSubscription> SubscribeAsync(CreateStatusSubscriptionRequest createStatusSubscriptionRequest, CancellationToken stoppingToken)
    {
        var now = timeProvider.GetUtcNow();
        var normalizedEmail = new Email(((string)createStatusSubscriptionRequest.Email).Trim().ToUpperInvariant());

        var confirmationToken = StatusSubscriptionTokenHelper.Generate();
        var unsubscribeToken = StatusSubscriptionTokenHelper.Generate();

        var existing = await statusSubscriptionRepository.GetByEmailAsync(normalizedEmail, stoppingToken);

        if (existing is not null)
        {
            await statusSubscriptionRepository.ReissueConfirmationAsync(
                existing.StatusSubscriptionId, StatusSubscriptionTokenHelper.Hash(confirmationToken), stoppingToken);

            await unitOfWork.SaveChangesAsync(stoppingToken);

            return new IssuedStatusSubscription
            {
                StatusSubscription = existing,
                ConfirmationToken = confirmationToken,

                UnsubscribeToken = unsubscribeToken
            };
        }

        var statusSubscription = new StatusSubscription
        {
            StatusSubscriptionId = new StatusSubscriptionId(Guid.NewGuid()),
            Email = createStatusSubscriptionRequest.Email,
            ConfirmedAt = null,
            UnsubscribedAt = null,
            CreatedAt = new CreatedAt(now),
            UpdatedAt = new UpdatedAt(now)
        };

        await statusSubscriptionRepository.AddAsync(
            statusSubscription,
            normalizedEmail,
            StatusSubscriptionTokenHelper.Hash(confirmationToken),
            StatusSubscriptionTokenHelper.Hash(unsubscribeToken),
            stoppingToken);

        await unitOfWork.SaveChangesAsync(stoppingToken);

        return new IssuedStatusSubscription
        {
            StatusSubscription = statusSubscription,
            ConfirmationToken = confirmationToken,
            UnsubscribeToken = unsubscribeToken
        };
    }

    public async Task<StatusSubscription> ConfirmAsync(ConfirmStatusSubscriptionRequest confirmStatusSubscriptionRequest, CancellationToken stoppingToken)
    {
        var matches = await statusSubscriptionRepository.MatchesConfirmationTokenAsync(
            confirmStatusSubscriptionRequest.StatusSubscriptionId,
            StatusSubscriptionTokenHelper.Hash(confirmStatusSubscriptionRequest.Token),
            stoppingToken);

        if (!matches)
        {
            throw new InvalidStatusSubscriptionTokenException();
        }

        await statusSubscriptionRepository.ConfirmAsync(confirmStatusSubscriptionRequest.StatusSubscriptionId, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);

        var statusSubscription = await statusSubscriptionRepository.GetAsync(
            confirmStatusSubscriptionRequest.StatusSubscriptionId, stoppingToken);

        return statusSubscription ?? throw new InvalidStatusSubscriptionTokenException();
    }

    public async Task UnsubscribeAsync(UnsubscribeStatusSubscriptionRequest unsubscribeStatusSubscriptionRequest, CancellationToken stoppingToken)
    {
        var matches = await statusSubscriptionRepository.MatchesUnsubscribeTokenAsync(
            unsubscribeStatusSubscriptionRequest.StatusSubscriptionId,
            StatusSubscriptionTokenHelper.Hash(unsubscribeStatusSubscriptionRequest.Token),
            stoppingToken);

        if (!matches)
        {
            throw new InvalidStatusSubscriptionTokenException();
        }

        await statusSubscriptionRepository.UnsubscribeAsync(unsubscribeStatusSubscriptionRequest.StatusSubscriptionId, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);
    }

    public Task<List<StatusSubscription>> ListDeliverableAsync(CancellationToken stoppingToken)
    {
        return statusSubscriptionRepository.ListDeliverableAsync(stoppingToken);
    }
}
