using Core.Configs;
using Core.Enums;
using Core.Interfaces.Repositories;
using Core.Models.Common;
using Core.Models.OutboxMessages;
using Core.ValueObjects;
using Infrastructure.Entities;
using Infrastructure.Expressions;
using Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Infrastructure.Repositories;

public class OutboxMessageRepository(
    TransactionMonitoringDbContext context,
    IOptions<AppConfig> appConfig,
    TimeProvider timeProvider)
    : PagedRepository<ListWebhookDeliveryRequest, OutboxMessageEntity, OutboxMessage>, IOutboxMessageRepository
{
    public async Task AddAsync(OutboxMessage outboxMessage, CancellationToken stoppingToken)
    {
        var outboxMessageEntity = outboxMessage.ToEntity();
        await context.OutboxMessages.AddAsync(outboxMessageEntity, stoppingToken);
    }

    public async Task AddRangeAsync(List<OutboxMessage> outboxMessages, CancellationToken stoppingToken)
    {
        var outboxMessageEntities = outboxMessages.Select(x => x.ToEntity());
        await context.OutboxMessages.AddRangeAsync(outboxMessageEntities, stoppingToken);
    }

    public async Task<OutboxMessage> GetAsync(OutboxMessageId outboxMessageId, CancellationToken stoppingToken)
    {
        var outboxMessageEntity = await context.OutboxMessages
            .FirstAsync(x => x.OutboxMessageId == outboxMessageId && !x.IsDeleted, stoppingToken);

        return outboxMessageEntity.ToModel();
    }

    public async Task<List<OutboxMessage>> ListPendingAsync(CancellationToken stoppingToken)
    {
        var outboxMessageEntities = await context.OutboxMessages
            .Where(x => !x.IsProcessed
                        && !x.IsDeleted
                        && x.Attempts < appConfig.Value.BackgroundServiceConfig.OutboxMaxAttempts
                        && (timeProvider.GetUtcNow() - x.UpdatedAt).TotalSeconds >= Math.Pow(2, x.Attempts))
            .ToListAsync(stoppingToken);

        return outboxMessageEntities.Select(x => x.ToModel()).ToList();
    }

    public async Task<Exists> ExistsAsync(OutboxMessageId outboxMessageId, IncludeDeleted includeDeleted, CancellationToken stoppingToken)
    {
        var exists = await context.OutboxMessages
            .AnyAsync(x => x.OutboxMessageId == outboxMessageId &&
                           (includeDeleted || x.IsDeleted == false), stoppingToken);

        return new Exists(exists);
    }

    public async Task UpdateAsync(UpdateOutboxMessageRequest updateOutboxMessageRequest, CancellationToken stoppingToken)
    {
        var outboxMessageEntity = await context.OutboxMessages
            .FirstAsync(x => x.OutboxMessageId == updateOutboxMessageRequest.OutboxMessageId && !x.IsDeleted, stoppingToken);

        ApplyAttempt(outboxMessageEntity, updateOutboxMessageRequest);
        outboxMessageEntity.UpdatedAt = timeProvider.GetUtcNow();

        context.OutboxMessages.Update(outboxMessageEntity);
    }

    public async Task ProcessAsync(OutboxMessageId outboxMessageId, UpdateOutboxMessageRequest updateOutboxMessageRequest, CancellationToken stoppingToken)
    {
        var outboxMessageEntity = await context.OutboxMessages
            .FirstAsync(x => x.OutboxMessageId == outboxMessageId && !x.IsDeleted, stoppingToken);

        ApplyAttempt(outboxMessageEntity, updateOutboxMessageRequest);
        outboxMessageEntity.IsProcessed = true;
        outboxMessageEntity.ProcessedAt = timeProvider.GetUtcNow();
        outboxMessageEntity.UpdatedAt = timeProvider.GetUtcNow();

        context.OutboxMessages.Update(outboxMessageEntity);
    }

    public async Task SuppressAsync(OutboxMessageId outboxMessageId, CancellationToken stoppingToken)
    {
        var outboxMessageEntity = await context.OutboxMessages
            .FirstAsync(x => x.OutboxMessageId == outboxMessageId && !x.IsDeleted, stoppingToken);

        outboxMessageEntity.IsProcessed = true;
        outboxMessageEntity.ProcessedAt = timeProvider.GetUtcNow();
        outboxMessageEntity.SuppressedAt = timeProvider.GetUtcNow();
        outboxMessageEntity.UpdatedAt = timeProvider.GetUtcNow();

        context.OutboxMessages.Update(outboxMessageEntity);
    }

    public async Task<Exists> ExistsForOrganizationAsync(OrganizationId organizationId, OutboxMessageId outboxMessageId, CancellationToken stoppingToken)
    {
        var exists = await context.OutboxMessages
            .AnyAsync(x => x.OutboxMessageId == outboxMessageId && x.OrganizationId == organizationId && !x.IsDeleted, stoppingToken);

        return new Exists(exists);
    }

    public async Task<List<OutboxMessage>> ListForReplayAsync(
        ReplayWebhookDeliveryRangeRequest replayWebhookDeliveryRangeRequest, Count limit, CancellationToken stoppingToken)
    {
        var outboxMessageEntities = await ForReplay(replayWebhookDeliveryRangeRequest)
            .OrderBy(x => x.CreatedAt)
            .Take(limit)
            .ToListAsync(stoppingToken);

        return outboxMessageEntities.ConvertAll(x => x.ToModel());
    }

    public async Task<Count> CountForReplayAsync(
        ReplayWebhookDeliveryRangeRequest replayWebhookDeliveryRangeRequest, CancellationToken stoppingToken)
    {
        var count = await ForReplay(replayWebhookDeliveryRangeRequest).CountAsync(stoppingToken);

        return new Count(count);
    }

    public async Task<WebhookDeliveryTally> TallyAsync(
        WebhookConfigurationId webhookConfigurationId, PeriodStart from, PeriodEnd to, CancellationToken stoppingToken)
    {
        var counts = await context.OutboxMessages
            .Where(x => x.WebhookConfigurationId == webhookConfigurationId &&
                        x.CreatedAt >= from &&
                        x.CreatedAt <= to &&
                        !x.IsDeleted)
            .GroupBy(x => new { x.IsProcessed, Exhausted = x.Attempts >= appConfig.Value.BackgroundServiceConfig.OutboxMaxAttempts })
            .Select(x => new { x.Key.IsProcessed, x.Key.Exhausted, Count = x.Count() })
            .ToListAsync(stoppingToken);

        return new WebhookDeliveryTally
        {
            Delivered = new Count(counts.Where(x => x.IsProcessed).Sum(x => x.Count)),
            Failed = new Count(counts.Where(x => !x.IsProcessed && x.Exhausted).Sum(x => x.Count)),

            Pending = new Count(counts.Where(x => !x.IsProcessed && !x.Exhausted).Sum(x => x.Count))
        };
    }

    protected override Task<List<OutboxMessage>> MapPageAsync(List<OutboxMessageEntity> items, ListWebhookDeliveryRequest request, CancellationToken stoppingToken)
    {
        return Task.FromResult(items.ConvertAll(x => x.ToModel()));
    }

    protected override IOrderByExpression<OutboxMessageEntity> OrderByExpression()
    {
        return new OrderByExpression<OutboxMessageEntity, DateTimeOffset>(x => x.CreatedAt);
    }

    protected override IQueryable<OutboxMessageEntity> FilterByPagedRequest(ListWebhookDeliveryRequest request)
    {
        var queryable = context.OutboxMessages
            .Where(x => x.OrganizationId == request.OrganizationId &&
                        x.ChannelId == (int)NotificationChannel.Webhook &&
                        !x.IsDeleted);

        if (request.WebhookConfigurationId.HasValue)
        {
            queryable = queryable.Where(x => x.WebhookConfigurationId == request.WebhookConfigurationId.Value);
        }

        if (request.Event.HasValue)
        {
            queryable = queryable.Where(x => x.Event == request.Event.Value.ToString());
        }

        if (request.IsDelivered.HasValue)
        {
            queryable = request.IsDelivered.Value
                ? queryable.Where(x => x.IsProcessed && x.SuppressedAt == null)
                : queryable.Where(x => !x.IsProcessed || x.SuppressedAt != null);
        }

        return queryable;
    }

    protected override IQueryable<OutboxMessageEntity> FilterByDateRange(IQueryable<OutboxMessageEntity> queryable, DatedRequest datedRequest)
    {
        if (datedRequest.FromDate != DateOnly.MinValue)
        {
            queryable = queryable.Where(x => DateOnly.FromDateTime(x.CreatedAt.UtcDateTime) >= datedRequest.FromDate);
        }

        if (datedRequest.ToDate != DateOnly.MaxValue)
        {
            queryable = queryable.Where(x => DateOnly.FromDateTime(x.CreatedAt.UtcDateTime) <= datedRequest.ToDate);
        }

        return queryable;
    }

    private IQueryable<OutboxMessageEntity> ForReplay(ReplayWebhookDeliveryRangeRequest replayWebhookDeliveryRangeRequest)
    {
        var queryable = context.OutboxMessages
            .Where(x => x.OrganizationId == replayWebhookDeliveryRangeRequest.OrganizationId &&
                        x.ChannelId == (int)NotificationChannel.Webhook &&
                        x.CreatedAt >= replayWebhookDeliveryRangeRequest.From &&
                        x.CreatedAt <= replayWebhookDeliveryRangeRequest.To &&
                        x.SuppressedAt == null &&
                        !x.IsDeleted);

        if (replayWebhookDeliveryRangeRequest.WebhookConfigurationId.HasValue)
        {
            queryable = queryable.Where(x => x.WebhookConfigurationId == replayWebhookDeliveryRangeRequest.WebhookConfigurationId.Value);
        }

        if (replayWebhookDeliveryRangeRequest.Event.HasValue)
        {
            queryable = queryable.Where(x => x.Event == replayWebhookDeliveryRangeRequest.Event.Value.ToString());
        }

        if (!replayWebhookDeliveryRangeRequest.IncludeDelivered)
        {
            queryable = queryable.Where(x => !x.IsProcessed);
        }

        return queryable;
    }

    private static void ApplyAttempt(OutboxMessageEntity outboxMessageEntity, UpdateOutboxMessageRequest updateOutboxMessageRequest)
    {
        outboxMessageEntity.Attempts = updateOutboxMessageRequest.Attempts;
        outboxMessageEntity.LastAttemptedAt = updateOutboxMessageRequest.LastAttemptedAt;
        outboxMessageEntity.ResponseStatusCode = updateOutboxMessageRequest.ResponseStatusCode;
        outboxMessageEntity.ResponseBody = updateOutboxMessageRequest.ResponseBody?.ToString();
        outboxMessageEntity.Latency = updateOutboxMessageRequest.Latency;
    }
}
