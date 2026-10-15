using Core.Constants;
using Core.Localization;
using Core.Models.Notifications;
using Core.Models.Status;

namespace Core.Helpers;

public static class StatusNotificationMessageBuilder
{
    public static NotificationMessage ForIncident(
        Incident incident, IncidentUpdate incidentUpdate, Translator translator, DateTimeOffset timestamp)
    {
        var components = string.Join(", ", incident.Components);

        return new NotificationMessage
        {
            Subject = translator.Text(TranslationKeys.NotificationIncidentSubject,
                translator.IncidentStatus(incident.Status), incident.IncidentNumber.ToString(), incident.Title.ToString()),
            Body = translator.Text(TranslationKeys.NotificationIncidentBody,
                incidentUpdate.Body.ToString(), components,
                translator.IncidentImpact(incident.Impact), translator.Moment(incident.StartedAt)),
            CaseId = null,
            Event = WebhookEvents.IncidentUpdated,
            Timestamp = timestamp
        };
    }

    public static NotificationMessage ForMaintenanceWindow(
        MaintenanceWindow maintenanceWindow, Translator translator, DateTimeOffset timestamp)
    {
        var components = string.Join(", ", maintenanceWindow.Components);

        return new NotificationMessage
        {
            Subject = translator.Text(TranslationKeys.NotificationMaintenanceSubject, maintenanceWindow.Title.ToString()),
            Body = translator.Text(TranslationKeys.NotificationMaintenanceBody,
                maintenanceWindow.Description.ToString(), components,
                translator.Moment(maintenanceWindow.ScheduledStart), translator.Moment(maintenanceWindow.ScheduledEnd)),
            CaseId = null,
            Event = WebhookEvents.MaintenanceScheduled,
            Timestamp = timestamp
        };
    }
}
