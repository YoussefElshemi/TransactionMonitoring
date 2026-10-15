using Core.Helpers;
using Core.Interfaces.Services;
using Core.Localization;
using Core.Models.Notifications;
using Core.Models.Status;
using Core.ValueObjects;
using Serilog;

namespace Infrastructure.Services;

public class EmailStatusNotifier(
    IEmailSender emailSender,
    IStatusSubscriptionService statusSubscriptionService,
    IDisplayPreferenceService displayPreferenceService,
    TimeProvider timeProvider,
    ILogger logger) : IStatusNotifier
{
    public async Task<Count> NotifySubscribersAsync(Incident incident, IncidentUpdate incidentUpdate, CancellationToken stoppingToken)
    {
        var subscriptions = await statusSubscriptionService.ListDeliverableAsync(stoppingToken);

        var translator = new Translator(displayPreferenceService.Fallback());
        var notificationMessage = StatusNotificationMessageBuilder.ForIncident(incident, incidentUpdate, translator, timeProvider.GetUtcNow());

        return await SendAllAsync(subscriptions.Select(x => new Destination(x.Email)), notificationMessage, stoppingToken);
    }

    public async Task<Count> NotifySubscribersAsync(MaintenanceWindow maintenanceWindow, CancellationToken stoppingToken)
    {
        var subscriptions = await statusSubscriptionService.ListDeliverableAsync(stoppingToken);
        var translator = new Translator(displayPreferenceService.Fallback());
        var notificationMessage = StatusNotificationMessageBuilder.ForMaintenanceWindow(maintenanceWindow, translator, timeProvider.GetUtcNow());

        return await SendAllAsync(subscriptions.Select(x => new Destination(x.Email)), notificationMessage, stoppingToken);
    }

    private async Task<Count> SendAllAsync(IEnumerable<Destination> destinations, NotificationMessage notificationMessage, CancellationToken stoppingToken)
    {
        var sent = 0;

        foreach (var destination in destinations)
        {
            try
            {
                var result = await emailSender.SendAsync(destination, notificationMessage, stoppingToken);

                if (result)
                {
                    sent++;
                }
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.Warning(
                    exception,
                    "Sending the status notification to {Destination} failed: {Message}",
                    destination.ToString(),
                    exception.Message);
            }
        }

        return new Count(sent);
    }
}
