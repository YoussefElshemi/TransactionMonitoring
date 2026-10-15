using Core.Configs;
using Core.Enums;
using Core.Helpers;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Mappers;
using Core.Mappers.Notifications;
using Core.Models.OutboxMessages;
using Core.Models.WebhookConfigurations;
using Core.ValueObjects;
using Microsoft.Extensions.Options;

namespace Core.Services;

public class WebhookHealthService(
    IWebhookConfigurationRepository webhookConfigurationRepository,
    IOutboxMessageRepository outboxMessageRepository,
    IDisplayPreferenceService displayPreferenceService,
    INotificationSubscriptionRepository notificationSubscriptionRepository,
    IOptions<AppConfig> appConfig,
    TimeProvider timeProvider) : IWebhookHealthService
{
    public async Task RecordOutcomeAsync(OutboxMessage outboxMessage, Result result, Attempts attempts, CancellationToken stoppingToken)
    {
        if (outboxMessage.Channel != NotificationChannel.Webhook || outboxMessage.WebhookConfigurationId is null)
        {
            return;
        }

        var webhookConfigurationId = outboxMessage.WebhookConfigurationId.Value;

        if (result)
        {
            await webhookConfigurationRepository.RecordSuccessAsync(webhookConfigurationId, stoppingToken);

            return;
        }

        if (attempts < appConfig.Value.BackgroundServiceConfig.OutboxMaxAttempts)
        {
            return;
        }

        var disabledReason = new DisabledReason(
            $"Disabled automatically after {appConfig.Value.WebhookConfig.AutoDisableAfterConsecutiveFailures} " +
            $"consecutive failed deliveries to {outboxMessage.Destination}.");

        var outcome = await webhookConfigurationRepository.RecordFailureAsync(
            webhookConfigurationId,
            new ConsecutiveFailures(appConfig.Value.WebhookConfig.AutoDisableAfterConsecutiveFailures),
            disabledReason,
            stoppingToken);

        if (!outcome.WasDisabled)
        {
            return;
        }

        if (outboxMessage.OrganizationId is null)
        {
            return;
        }

        await NotifyDisabledAsync(outboxMessage.OrganizationId.Value, outboxMessage.Mode, disabledReason, stoppingToken);
    }

    public async Task<WebhookEndpointHealth> GetAsync(WebhookConfigurationId webhookConfigurationId, CancellationToken stoppingToken)
    {
        var webhookConfiguration = await webhookConfigurationRepository.GetAsync(webhookConfigurationId, stoppingToken);

        var now = timeProvider.GetUtcNow();

        var to = new PeriodEnd(now);
        var from = new PeriodStart(now.AddDays(-appConfig.Value.WebhookConfig.HealthWindowDays));

        var tally = await outboxMessageRepository.TallyAsync(webhookConfigurationId, from, to, stoppingToken);

        return new WebhookEndpointHealth
        {
            WebhookConfigurationId = webhookConfiguration.WebhookConfigurationId,
            OrganizationId = webhookConfiguration.OrganizationId,
            IsEnabled = webhookConfiguration.IsEnabled,
            ConsecutiveFailures = webhookConfiguration.ConsecutiveFailures,
            LastSucceededAt = webhookConfiguration.LastSucceededAt,
            LastFailedAt = webhookConfiguration.LastFailedAt,
            DisabledAt = webhookConfiguration.DisabledAt,
            DisabledReason = webhookConfiguration.DisabledReason,
            From = from,
            To = to,
            Delivered = tally.Delivered,
            Failed = tally.Failed,
            Pending = tally.Pending
        };
    }

    private async Task NotifyDisabledAsync(OrganizationId organizationId, Mode mode, DisabledReason disabledReason, CancellationToken stoppingToken)
    {
        var subscriptions = await notificationSubscriptionRepository.ListEnabledAsync(organizationId, stoppingToken);

        var matched = subscriptions.Where(x => x.Channel != NotificationChannel.Webhook).ToList();

        if (matched.Count == 0)
        {
            return;
        }

        var preferences = await displayPreferenceService.ForOrganizationAsync(organizationId, stoppingToken);

        // Staged, not saved: the outbox pass owns the transaction.
        var outboxMessages = matched
            .ToOutboxMessageRequests(
                preferences,
                translator => NotificationMessageBuilder.ForDisabledWebhookConfiguration(disabledReason, translator, timeProvider.GetUtcNow()))
            .Select(x => x.ToModel(timeProvider, mode))
            .ToList();

        await outboxMessageRepository.AddRangeAsync(outboxMessages, stoppingToken);
    }
}
