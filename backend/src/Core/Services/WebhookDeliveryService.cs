using Core.Configs;
using Core.Enums;
using Core.Exceptions;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Mappers;
using Core.Models.Common;
using Core.Models.OutboxMessages;
using Core.ValueObjects;
using Microsoft.Extensions.Options;

namespace Core.Services;

public class WebhookDeliveryService(
    IOutboxMessageRepository outboxMessageRepository,
    IUnitOfWork unitOfWork,
    IOptions<AppConfig> appConfig,
    TimeProvider timeProvider,
    IModeContext modeContext) : IWebhookDeliveryService
{
    public Task<PagedResponse<OutboxMessage>> ListAsync(ListWebhookDeliveryRequest listWebhookDeliveryRequest, CancellationToken stoppingToken)
    {
        return outboxMessageRepository.ListAsync(listWebhookDeliveryRequest, stoppingToken);
    }

    public Task<TotalCount> GetCountAsync(ListWebhookDeliveryRequest listWebhookDeliveryRequest, CancellationToken stoppingToken)
    {
        return outboxMessageRepository.GetCountAsync(listWebhookDeliveryRequest, stoppingToken);
    }

    public Task<OutboxMessage> GetAsync(OutboxMessageId outboxMessageId, CancellationToken stoppingToken)
    {
        return outboxMessageRepository.GetAsync(outboxMessageId, stoppingToken);
    }

    public Task<Exists> ExistsAsync(OrganizationId organizationId, OutboxMessageId outboxMessageId, CancellationToken stoppingToken)
    {
        return outboxMessageRepository.ExistsForOrganizationAsync(organizationId, outboxMessageId, stoppingToken);
    }

    public async Task<OutboxMessage> ReplayAsync(OutboxMessageId outboxMessageId, CancellationToken stoppingToken)
    {
        var original = await outboxMessageRepository.GetAsync(outboxMessageId, stoppingToken);

        var replay = BuildReplay(original);

        await outboxMessageRepository.AddAsync(replay, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);

        return await outboxMessageRepository.GetAsync(replay.OutboxMessageId, stoppingToken);
    }

    public async Task<WebhookReplayBatch> ReplayRangeAsync(ReplayWebhookDeliveryRangeRequest replayWebhookDeliveryRangeRequest, CancellationToken stoppingToken)
    {
        if (replayWebhookDeliveryRangeRequest.To < (DateTimeOffset)replayWebhookDeliveryRangeRequest.From)
        {
            throw new InvalidTimeRangeException("replay window");
        }

        var limit = new Count(appConfig.Value.WebhookConfig.ReplayRangeLimit);

        var matched = await outboxMessageRepository.CountForReplayAsync(replayWebhookDeliveryRangeRequest, stoppingToken);

        var originals = await outboxMessageRepository.ListForReplayAsync(replayWebhookDeliveryRangeRequest, limit, stoppingToken);

        var replays = originals.ConvertAll(BuildReplay);

        await outboxMessageRepository.AddRangeAsync(replays, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);

        return new WebhookReplayBatch
        {
            OrganizationId = replayWebhookDeliveryRangeRequest.OrganizationId,
            From = replayWebhookDeliveryRangeRequest.From,
            To = replayWebhookDeliveryRangeRequest.To,
            Matched = matched,
            Queued = new Count(replays.Count),
            HasMore = new HasMore(matched > replays.Count)
        };
    }

    private OutboxMessage BuildReplay(OutboxMessage original)
    {
        if (original.Channel != NotificationChannel.Webhook)
        {
            throw new NotReplayableException(original.Channel.ToString());
        }

        ArgumentNullException.ThrowIfNull(original.OrganizationId);

        return new CreateOutboxMessageRequest
        {
            OrganizationId = original.OrganizationId.Value,
            Payload = original.Payload,
            Channel = original.Channel,
            Destination = original.Destination,
            SecretKey = original.SecretKey,
            WebhookConfigurationId = original.WebhookConfigurationId,
            Event = original.Event,
            PayloadVersion = original.PayloadVersion,
            ReplayOfOutboxMessageId = original.ReplayOfOutboxMessageId ?? original.OutboxMessageId
        }.ToModel(timeProvider, modeContext.CurrentOrLive);
    }
}
