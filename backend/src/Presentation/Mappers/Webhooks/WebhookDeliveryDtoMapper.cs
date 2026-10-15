using Core.Extensions;
using Core.Models.OutboxMessages;
using Core.ValueObjects;
using Presentation.Models.Webhooks;

namespace Presentation.Mappers.Webhooks;

public static class WebhookDeliveryDtoMapper
{
    public static WebhookDeliveryResponseDto ToDeliveryDto(this OutboxMessage outboxMessage)
    {
        return new WebhookDeliveryResponseDto
        {
            Mode = outboxMessage.Mode.ToString(),
            OutboxMessageId = outboxMessage.OutboxMessageId,
            OrganizationId = outboxMessage.OrganizationId,
            WebhookConfigurationId = outboxMessage.WebhookConfigurationId,
            Event = outboxMessage.Event?.ToString(),
            PayloadVersion = outboxMessage.PayloadVersion,
            Destination = outboxMessage.Destination.ToString(),
            Payload = outboxMessage.Payload.ToString().FromBase64String(),
            Attempts = outboxMessage.Attempts,
            LastAttemptedAt = outboxMessage.LastAttemptedAt,
            ResponseStatusCode = outboxMessage.ResponseStatusCode,
            ResponseBody = outboxMessage.ResponseBody?.ToString(),
            LatencyMilliseconds = outboxMessage.Latency,

            IsDelivered = outboxMessage.IsProcessed && outboxMessage.SuppressedAt is null,
            DeliveredAt = outboxMessage.SuppressedAt is null && outboxMessage.ProcessedAt.HasValue
                ? outboxMessage.ProcessedAt.Value
                : null,
            SuppressedAt = outboxMessage.SuppressedAt.HasValue ? outboxMessage.SuppressedAt.Value : null,
            ReplayOfOutboxMessageId = outboxMessage.ReplayOfOutboxMessageId,
            CreatedAt = outboxMessage.CreatedAt,
            UpdatedAt = outboxMessage.UpdatedAt
        };
    }

    public static WebhookReplayBatchResponseDto ToDto(this WebhookReplayBatch webhookReplayBatch)
    {
        return new WebhookReplayBatchResponseDto
        {
            OrganizationId = webhookReplayBatch.OrganizationId,
            From = webhookReplayBatch.From,
            To = webhookReplayBatch.To,
            Matched = webhookReplayBatch.Matched,
            Queued = webhookReplayBatch.Queued,
            HasMore = webhookReplayBatch.HasMore
        };
    }

    public static ListWebhookDeliveryRequest ToModel(this ListWebhookDeliveryRequestDto listWebhookDeliveryRequestDto)
    {
        return new ListWebhookDeliveryRequest
        {
            OrganizationId = new OrganizationId(listWebhookDeliveryRequestDto.OrganizationId),
            WebhookConfigurationId = listWebhookDeliveryRequestDto.WebhookConfigurationId.HasValue
                ? new WebhookConfigurationId(listWebhookDeliveryRequestDto.WebhookConfigurationId.Value)
                : null,
            Event = listWebhookDeliveryRequestDto.Event is null ? null : new WebhookEvent(listWebhookDeliveryRequestDto.Event),
            IsDelivered = listWebhookDeliveryRequestDto.IsDelivered.HasValue
                ? new IsDelivered(listWebhookDeliveryRequestDto.IsDelivered.Value)
                : null,
            PageNumber = new PageNumber(listWebhookDeliveryRequestDto.PageNumber),
            PageSize = new PageSize(listWebhookDeliveryRequestDto.PageSize),
            FromDate = new DateRange(listWebhookDeliveryRequestDto.FromDate),
            ToDate = new DateRange(listWebhookDeliveryRequestDto.ToDate)
        };
    }

    public static ListWebhookDeliveryRequest ToModel(this ListMyWebhookDeliveryRequestDto listMyWebhookDeliveryRequestDto)
    {
        return new ListWebhookDeliveryRequest
        {
            OrganizationId = new OrganizationId(listMyWebhookDeliveryRequestDto.OrganizationId),
            WebhookConfigurationId = listMyWebhookDeliveryRequestDto.WebhookConfigurationId.HasValue
                ? new WebhookConfigurationId(listMyWebhookDeliveryRequestDto.WebhookConfigurationId.Value)
                : null,
            Event = listMyWebhookDeliveryRequestDto.Event is null ? null : new WebhookEvent(listMyWebhookDeliveryRequestDto.Event),
            IsDelivered = listMyWebhookDeliveryRequestDto.IsDelivered.HasValue
                ? new IsDelivered(listMyWebhookDeliveryRequestDto.IsDelivered.Value)
                : null,
            PageNumber = new PageNumber(listMyWebhookDeliveryRequestDto.PageNumber),
            PageSize = new PageSize(listMyWebhookDeliveryRequestDto.PageSize),
            FromDate = new DateRange(listMyWebhookDeliveryRequestDto.FromDate),
            ToDate = new DateRange(listMyWebhookDeliveryRequestDto.ToDate)
        };
    }

    public static ReplayWebhookDeliveryRangeRequest ToModel(this ReplayWebhookDeliveryRangeRequestDto replayWebhookDeliveryRangeRequestDto)
    {
        return new ReplayWebhookDeliveryRangeRequest
        {
            OrganizationId = new OrganizationId(replayWebhookDeliveryRangeRequestDto.OrganizationId),
            From = new PeriodStart(replayWebhookDeliveryRangeRequestDto.Body.From),
            To = new PeriodEnd(replayWebhookDeliveryRangeRequestDto.Body.To),
            WebhookConfigurationId = replayWebhookDeliveryRangeRequestDto.Body.WebhookConfigurationId.HasValue
                ? new WebhookConfigurationId(replayWebhookDeliveryRangeRequestDto.Body.WebhookConfigurationId.Value)
                : null,
            Event = replayWebhookDeliveryRangeRequestDto.Body.Event is null
                ? null
                : new WebhookEvent(replayWebhookDeliveryRangeRequestDto.Body.Event),
            IncludeDelivered = new IncludeDelivered(replayWebhookDeliveryRangeRequestDto.Body.IncludeDelivered)
        };
    }
}
