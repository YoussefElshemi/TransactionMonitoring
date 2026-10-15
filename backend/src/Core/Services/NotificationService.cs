using Core.Configs;
using Core.Constants;
using Core.Helpers;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Mappers.Notifications;
using Core.Mappers.Webhooks;
using Core.Models.Localization;
using Core.Models.NotificationSubscriptions;
using Core.Models.OutboxMessages;
using Core.Models.WebhookConfigurations;
using Core.ValueObjects;
using Microsoft.Extensions.Options;

namespace Core.Services;

public class NotificationService(
    ICaseRepository caseRepository,
    ISuspiciousActivityReportRepository suspiciousActivityReportRepository,
    INotificationSubscriptionRepository notificationSubscriptionRepository,
    IWebhookConfigurationRepository webhookConfigurationRepository,
    IOutboxMessageService outboxMessageService,
    IDisplayPreferenceService displayPreferenceService,
    IUnitOfWork unitOfWork,
    IOptions<AppConfig> appConfig,
    TimeProvider timeProvider) : INotificationService
{
    public async Task<Count> NotifyBreachedAsync(CancellationToken stoppingToken)
    {
        var batchSize = new Count(appConfig.Value.NotificationConfig.BreachBatchSize);

        var breachedCases = await caseRepository.ListBreachNotificationCandidatesAsync(batchSize, stoppingToken);

        if (breachedCases.Count == 0)
        {
            return new Count(0);
        }

        var subscriptionsByOrganization = new Dictionary<OrganizationId, List<NotificationSubscription>>();

        var preferencesByOrganization = new Dictionary<OrganizationId, DisplayPreferences>();

        var createOutboxMessageRequests = new List<CreateOutboxMessageRequest>();

        foreach (var breachedCase in breachedCases)
        {
            if (!subscriptionsByOrganization.TryGetValue(breachedCase.OrganizationId, out var subscriptions))
            {
                subscriptions = await notificationSubscriptionRepository.ListEnabledAsync(breachedCase.OrganizationId, stoppingToken);
                subscriptionsByOrganization[breachedCase.OrganizationId] = subscriptions;
            }

            var matched = subscriptions.ForBreach();

            if (matched.Count == 0)
            {
                continue;
            }

            if (!preferencesByOrganization.TryGetValue(breachedCase.OrganizationId, out var preferences))
            {
                preferences = await displayPreferenceService.ForOrganizationAsync(breachedCase.OrganizationId, stoppingToken);
                preferencesByOrganization[breachedCase.OrganizationId] = preferences;
            }

            createOutboxMessageRequests.AddRange(matched.ToOutboxMessageRequests(
                preferences,
                translator => NotificationMessageBuilder.ForBreachedCase(breachedCase, translator, timeProvider.GetUtcNow())));
        }

        createOutboxMessageRequests.AddRange(await BuildWebhooksAsync(
            breachedCases.Select(x => (x.OrganizationId, Payload: x.ToWebhook())).ToList(),
            WebhookEvents.CaseBreached,
            stoppingToken));

        await outboxMessageService.StageManyAsync(createOutboxMessageRequests, stoppingToken);

        await caseRepository.MarkBreachNotifiedAsync(breachedCases.ConvertAll(x => x.CaseId), stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);

        return new Count(breachedCases.Count);
    }

    private async Task<List<CreateOutboxMessageRequest>> BuildWebhooksAsync<T>(
        List<(OrganizationId OrganizationId, T Payload)> subjects, string @event, CancellationToken stoppingToken)
    {
        var createOutboxMessageRequests = new List<CreateOutboxMessageRequest>();

        var configurationsByOrganization = new Dictionary<OrganizationId, List<WebhookConfiguration>>();

        foreach (var (organizationId, payload) in subjects)
        {
            if (!configurationsByOrganization.TryGetValue(organizationId, out var webhookConfigurations))
            {
                webhookConfigurations = await webhookConfigurationRepository.GetAllAsync(organizationId, stoppingToken);
                configurationsByOrganization[organizationId] = webhookConfigurations;
            }

            createOutboxMessageRequests.AddRange(
                webhookConfigurations.ToOutboxMessageRequests(@event, payload, timeProvider.GetUtcNow()));
        }

        return createOutboxMessageRequests;
    }

    public async Task<Count> NotifyOverdueFilingsAsync(CancellationToken stoppingToken)
    {
        var batchSize = new Count(appConfig.Value.NotificationConfig.OverdueFilingBatchSize);

        var overdueReports = await suspiciousActivityReportRepository.ListOverdueNotificationCandidatesAsync(batchSize, stoppingToken);

        if (overdueReports.Count == 0)
        {
            return new Count(0);
        }

        var subscriptionsByOrganization = new Dictionary<OrganizationId, List<NotificationSubscription>>();
        var preferencesByOrganization = new Dictionary<OrganizationId, DisplayPreferences>();

        var createOutboxMessageRequests = new List<CreateOutboxMessageRequest>();

        foreach (var overdueReport in overdueReports)
        {
            if (!subscriptionsByOrganization.TryGetValue(overdueReport.OrganizationId, out var subscriptions))
            {
                subscriptions = await notificationSubscriptionRepository.ListEnabledAsync(overdueReport.OrganizationId, stoppingToken);
                subscriptionsByOrganization[overdueReport.OrganizationId] = subscriptions;
            }

            var matched = subscriptions.ForBreach();

            if (matched.Count == 0)
            {
                continue;
            }

            if (!preferencesByOrganization.TryGetValue(overdueReport.OrganizationId, out var preferences))
            {
                preferences = await displayPreferenceService.ForOrganizationAsync(overdueReport.OrganizationId, stoppingToken);
                preferencesByOrganization[overdueReport.OrganizationId] = preferences;
            }

            createOutboxMessageRequests.AddRange(matched.ToOutboxMessageRequests(
                preferences,
                translator => NotificationMessageBuilder.ForOverdueSuspiciousActivityReport(overdueReport, translator, timeProvider.GetUtcNow())));
        }

        createOutboxMessageRequests.AddRange(await BuildWebhooksAsync(
            overdueReports.Select(x => (x.OrganizationId, Payload: x.ToWebhook())).ToList(),
            WebhookEvents.SuspiciousActivityReportOverdue,
            stoppingToken));

        await outboxMessageService.StageManyAsync(createOutboxMessageRequests, stoppingToken);

        await suspiciousActivityReportRepository.MarkOverdueNotifiedAsync(
            overdueReports.ConvertAll(x => x.SuspiciousActivityReportId), stoppingToken);

        await unitOfWork.SaveChangesAsync(stoppingToken);

        return new Count(overdueReports.Count);
    }
}
