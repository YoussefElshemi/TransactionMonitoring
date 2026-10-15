using Core.Interfaces.Repositories;
using Core.Models.Common;
using Core.Models.WebhookConfigurations;
using Core.ValueObjects;
using Infrastructure.Entities;
using Infrastructure.Expressions;
using Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class WebhookConfigurationRepository(
    TransactionMonitoringDbContext context,
    TimeProvider timeProvider)
    : PagedRepository<ListOrganizationWebhookConfigurationRequest, WebhookConfigurationEntity, WebhookConfiguration>, IWebhookConfigurationRepository
{
    public async Task AddAsync(WebhookConfiguration webhookConfiguration, CancellationToken stoppingToken)
    {
        var webhookConfigurationEntity = webhookConfiguration.ToEntity();
        await context.WebhookConfigurations.AddAsync(webhookConfigurationEntity, stoppingToken);
    }

    public async Task<WebhookConfiguration> GetAsync(WebhookConfigurationId webhookConfigurationId, CancellationToken stoppingToken)
    {
        var webhookConfigurationEntity = await context.WebhookConfigurations
            .Include(x => x.SubscribedEvents)
            .FirstAsync(x => x.WebhookConfigurationId == webhookConfigurationId && !x.IsDeleted, stoppingToken);

        return webhookConfigurationEntity.ToModel();
    }

    public async Task<List<WebhookConfiguration>> GetAllAsync(OrganizationId organizationId, CancellationToken stoppingToken)
    {
        var webhookConfigurationEntities = await context.WebhookConfigurations
            .Include(x => x.SubscribedEvents)
            .Where(x => x.OrganizationId == organizationId && x.IsEnabled && !x.IsDeleted)
            .ToListAsync(stoppingToken);

        return webhookConfigurationEntities.Select(x => x.ToModel()).ToList();
    }

    public async Task<Exists> ExistsAsync(OrganizationId organizationId, WebhookConfigurationId webhookConfigurationId, IncludeDeleted includeDeleted, CancellationToken stoppingToken)
    {
        var exists = await context.WebhookConfigurations
            .AnyAsync(x => x.OrganizationId == organizationId &&
                           x.WebhookConfigurationId == webhookConfigurationId &&
                           (includeDeleted || x.IsDeleted == false), stoppingToken);

        return new Exists(exists);
    }

    public async Task UpdateAsync(UpdateWebhookConfigurationRequest updateWebhookConfigurationRequest, CancellationToken stoppingToken)
    {
        var webhookConfigurationEntity = await context.WebhookConfigurations
            .Include(x => x.SubscribedEvents)
            .FirstAsync(x => x.WebhookConfigurationId == updateWebhookConfigurationRequest.WebhookConfigurationId && !x.IsDeleted, stoppingToken);

        webhookConfigurationEntity.EndpointUrl = updateWebhookConfigurationRequest.EndpointUrl?.ToString() ?? webhookConfigurationEntity.EndpointUrl;
        webhookConfigurationEntity.PayloadVersion = updateWebhookConfigurationRequest.PayloadVersion ?? webhookConfigurationEntity.PayloadVersion;

        if (updateWebhookConfigurationRequest.IsEnabled.HasValue)
        {
            webhookConfigurationEntity.IsEnabled = updateWebhookConfigurationRequest.IsEnabled.Value;

            if (updateWebhookConfigurationRequest.IsEnabled.Value)
            {
                webhookConfigurationEntity.ConsecutiveFailures = 0;
                webhookConfigurationEntity.DisabledAt = null;
                webhookConfigurationEntity.DisabledReason = null;
            }
        }

        if (updateWebhookConfigurationRequest.SubscribedEvents is not null)
        {
            ReplaceSubscribedEvents(webhookConfigurationEntity, updateWebhookConfigurationRequest.SubscribedEvents);
        }

        webhookConfigurationEntity.UpdatedAt = timeProvider.GetUtcNow();

        context.WebhookConfigurations.Update(webhookConfigurationEntity);
    }

    public async Task RotateSecretAsync(WebhookConfigurationId webhookConfigurationId, SecretKey secretKey, CancellationToken stoppingToken)
    {
        var webhookConfigurationEntity = await context.WebhookConfigurations
            .FirstAsync(x => x.WebhookConfigurationId == webhookConfigurationId && !x.IsDeleted, stoppingToken);

        webhookConfigurationEntity.SecretKey = secretKey;
        webhookConfigurationEntity.SecretKeyRotatedAt = timeProvider.GetUtcNow();
        webhookConfigurationEntity.UpdatedAt = timeProvider.GetUtcNow();

        context.WebhookConfigurations.Update(webhookConfigurationEntity);
    }

    public async Task RecordSuccessAsync(WebhookConfigurationId webhookConfigurationId, CancellationToken stoppingToken)
    {
        var webhookConfigurationEntity = await LoadForHealthAsync(webhookConfigurationId, stoppingToken);

        if (webhookConfigurationEntity is null)
        {
            return;
        }

        webhookConfigurationEntity.ConsecutiveFailures = 0;
        webhookConfigurationEntity.LastSucceededAt = timeProvider.GetUtcNow();
        webhookConfigurationEntity.UpdatedAt = timeProvider.GetUtcNow();

        context.WebhookConfigurations.Update(webhookConfigurationEntity);
    }

    public async Task<WebhookFailureOutcome> RecordFailureAsync(
        WebhookConfigurationId webhookConfigurationId,
        ConsecutiveFailures autoDisableThreshold,
        DisabledReason disabledReason,
        CancellationToken stoppingToken)
    {
        var webhookConfigurationEntity = await LoadForHealthAsync(webhookConfigurationId, stoppingToken);

        if (webhookConfigurationEntity is null)
        {
            return new WebhookFailureOutcome
            {
                ConsecutiveFailures = new ConsecutiveFailures(0),
                WasDisabled = new WasDisabled(false)
            };
        }

        webhookConfigurationEntity.ConsecutiveFailures += 1;
        webhookConfigurationEntity.LastFailedAt = timeProvider.GetUtcNow();

        var wasDisabled = webhookConfigurationEntity.ConsecutiveFailures >= autoDisableThreshold &&
                          !webhookConfigurationEntity.DisabledAt.HasValue;

        if (wasDisabled)
        {
            webhookConfigurationEntity.IsEnabled = false;
            webhookConfigurationEntity.DisabledAt = timeProvider.GetUtcNow();
            webhookConfigurationEntity.DisabledReason = disabledReason;
        }

        webhookConfigurationEntity.UpdatedAt = timeProvider.GetUtcNow();

        context.WebhookConfigurations.Update(webhookConfigurationEntity);

        return new WebhookFailureOutcome
        {
            ConsecutiveFailures = new ConsecutiveFailures(webhookConfigurationEntity.ConsecutiveFailures),
            WasDisabled = new WasDisabled(wasDisabled)
        };
    }

    public async Task DeleteAsync(WebhookConfigurationId webhookConfigurationId, CancellationToken stoppingToken)
    {
        var webhookConfigurationEntity = await context.WebhookConfigurations
            .FirstAsync(x => x.WebhookConfigurationId == webhookConfigurationId, stoppingToken);

        webhookConfigurationEntity.IsDeleted = true;
        webhookConfigurationEntity.DeletedAt = timeProvider.GetUtcNow();

        context.WebhookConfigurations.Update(webhookConfigurationEntity);
    }

    public async Task RecoverAsync(WebhookConfigurationId webhookConfigurationId, CancellationToken stoppingToken)
    {
        var webhookConfigurationEntity = await context.WebhookConfigurations
            .FirstAsync(x => x.WebhookConfigurationId == webhookConfigurationId, stoppingToken);

        webhookConfigurationEntity.IsDeleted = false;
        webhookConfigurationEntity.DeletedAt = null;

        context.WebhookConfigurations.Update(webhookConfigurationEntity);
    }

    protected override Task<List<WebhookConfiguration>> MapPageAsync(List<WebhookConfigurationEntity> items, ListOrganizationWebhookConfigurationRequest request, CancellationToken stoppingToken)
    {
        return Task.FromResult(items.ConvertAll(x => x.ToModel()));
    }

    protected override IOrderByExpression<WebhookConfigurationEntity> OrderByExpression()
    {
        return new OrderByExpression<WebhookConfigurationEntity, DateTimeOffset>(x => x.UpdatedAt);
    }

    protected override IQueryable<WebhookConfigurationEntity> FilterByPagedRequest(ListOrganizationWebhookConfigurationRequest listOrganizationWebhookConfigurationRequest)
    {
        var queryable = context.WebhookConfigurations
            .Include(x => x.SubscribedEvents)
            .Where(x => x.OrganizationId == listOrganizationWebhookConfigurationRequest.OrganizationId && !x.IsDeleted);

        if (listOrganizationWebhookConfigurationRequest.FriendlyName != null)
        {
            queryable = queryable
                .Where(x => EF.Functions.ILike(x.EndpointUrl, $"%{listOrganizationWebhookConfigurationRequest.FriendlyName}%"));
        }

        return queryable;
    }

    protected override IQueryable<WebhookConfigurationEntity> FilterByDateRange(IQueryable<WebhookConfigurationEntity> queryable, DatedRequest datedRequest)
    {
        if (datedRequest.FromDate != DateOnly.MinValue)
        {
            queryable = queryable
                .Where(x => DateOnly.FromDateTime(x.CreatedAt.UtcDateTime) >= datedRequest.FromDate);
        }

        if (datedRequest.ToDate != DateOnly.MaxValue)
        {
            queryable = queryable
                .Where(x => DateOnly.FromDateTime(x.CreatedAt.UtcDateTime) <= datedRequest.ToDate);
        }

        return queryable;
    }

    private async Task<WebhookConfigurationEntity?> LoadForHealthAsync(
        WebhookConfigurationId webhookConfigurationId, CancellationToken stoppingToken)
    {
        var tracked = context.WebhookConfigurations.Local
            .FirstOrDefault(x => x.WebhookConfigurationId == webhookConfigurationId);

        return tracked ?? await context.WebhookConfigurations
            .FirstOrDefaultAsync(x => x.WebhookConfigurationId == webhookConfigurationId && !x.IsDeleted, stoppingToken);
    }

    private void ReplaceSubscribedEvents(WebhookConfigurationEntity webhookConfigurationEntity, List<WebhookEvent> subscribedEvents)
    {
        var wanted = subscribedEvents.Select(x => x.ToString()).ToHashSet(StringComparer.Ordinal);

        var removed = webhookConfigurationEntity.SubscribedEvents.Where(x => !wanted.Contains(x.Event)).ToList();

        context.WebhookConfigurationEvents.RemoveRange(removed);

        var existing = webhookConfigurationEntity.SubscribedEvents.Select(x => x.Event).ToHashSet(StringComparer.Ordinal);

        var added = wanted.Where(x => !existing.Contains(x)).Select(x => new WebhookConfigurationEventEntity
        {
            WebhookConfigurationId = webhookConfigurationEntity.WebhookConfigurationId,
            Event = x,
            CreatedAt = timeProvider.GetUtcNow(),
            UpdatedAt = timeProvider.GetUtcNow()
        }).ToList();

        context.WebhookConfigurationEvents.AddRange(added);
    }
}
