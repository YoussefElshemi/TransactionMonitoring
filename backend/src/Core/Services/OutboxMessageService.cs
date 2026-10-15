using Core.Enums;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Mappers;
using Core.Models.OutboxMessages;
using Core.ValueObjects;

namespace Core.Services;

public class OutboxMessageService(
    IEnumerable<INotificationSender> notificationSenders,
    IOutboxMessageRepository outboxMessageRepository,
    IWebhookHealthService webhookHealthService,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IModeContext modeContext) : IOutboxMessageService
{
    private readonly Dictionary<NotificationChannel, INotificationSender> _notificationSenders =
        notificationSenders.ToDictionary(x => x.Channel);

    public async Task<OutboxMessage> CreateAsync(CreateOutboxMessageRequest createOutboxMessageRequest, CancellationToken stoppingToken)
    {
        var outboxMessage = createOutboxMessageRequest.ToModel(timeProvider, modeContext.CurrentOrLive);

        await outboxMessageRepository.AddAsync(outboxMessage, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);

        return await GetAsync(outboxMessage.OutboxMessageId, stoppingToken);
    }

    public async Task CreateManyAsync(List<CreateOutboxMessageRequest> createOutboxMessageRequests, CancellationToken stoppingToken)
    {
        await StageManyAsync(createOutboxMessageRequests, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);
    }

    public async Task StageManyAsync(List<CreateOutboxMessageRequest> createOutboxMessageRequests, CancellationToken stoppingToken)
    {
        var outboxMessages = createOutboxMessageRequests.Select(x => x.ToModel(timeProvider, modeContext.CurrentOrLive)).ToList();

        await outboxMessageRepository.AddRangeAsync(outboxMessages, stoppingToken);
    }

    public Task<OutboxMessage> GetAsync(OutboxMessageId outboxMessageId, CancellationToken stoppingToken)
    {
        return outboxMessageRepository.GetAsync(outboxMessageId, stoppingToken);
    }

    public Task<List<OutboxMessage>> ListPendingAsync(CancellationToken stoppingToken)
    {
        return outboxMessageRepository.ListPendingAsync(stoppingToken);
    }

    public Task<Exists> ExistsAsync(OutboxMessageId outboxMessageId, IncludeDeleted includeDeleted, CancellationToken stoppingToken)
    {
        return outboxMessageRepository.ExistsAsync(outboxMessageId, includeDeleted, stoppingToken);
    }

    public async Task UpdateAsync(UpdateOutboxMessageRequest updateOutboxMessageRequest, CancellationToken stoppingToken)
    {
        await outboxMessageRepository.UpdateAsync(updateOutboxMessageRequest, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);
    }

    public async Task ProcessAsync(OutboxMessage outboxMessage, CancellationToken stoppingToken)
    {
        // Left pending rather than failed: a missing sender means a bad deploy, and burning attempts would discard the messages.
        if (!_notificationSenders.TryGetValue(outboxMessage.Channel, out var notificationSender))
        {
            return;
        }

        if (ShouldSuppress(outboxMessage))
        {
            await outboxMessageRepository.SuppressAsync(outboxMessage.OutboxMessageId, stoppingToken);
            await unitOfWork.SaveChangesAsync(stoppingToken);

            return;
        }

        var deliveryResult = await notificationSender.SendAsync(outboxMessage, stoppingToken);

        var attempts = outboxMessage.Attempts + 1;

        var updateOutboxMessageRequest = new UpdateOutboxMessageRequest
        {
            OutboxMessageId = outboxMessage.OutboxMessageId,
            Attempts = attempts,
            LastAttemptedAt = new LastAttemptedAt(timeProvider.GetUtcNow()),
            ResponseStatusCode = deliveryResult.StatusCode,
            ResponseBody = deliveryResult.ResponseBody,
            Latency = deliveryResult.Latency
        };

        if (deliveryResult.Result)
        {
            await outboxMessageRepository.ProcessAsync(outboxMessage.OutboxMessageId, updateOutboxMessageRequest, stoppingToken);
        }
        else
        {
            await outboxMessageRepository.UpdateAsync(updateOutboxMessageRequest, stoppingToken);
        }

        await webhookHealthService.RecordOutcomeAsync(outboxMessage, deliveryResult.Result, attempts, stoppingToken);

        await unitOfWork.SaveChangesAsync(stoppingToken);
    }

    private static bool ShouldSuppress(OutboxMessage outboxMessage)
    {
        return outboxMessage.Mode == Mode.Test && outboxMessage.Channel != NotificationChannel.Webhook;
    }
}
