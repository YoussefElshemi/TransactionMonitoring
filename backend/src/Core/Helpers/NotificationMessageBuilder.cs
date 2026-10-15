using Core.Constants;
using Core.Localization;
using Core.Models.Cases;
using Core.Models.Notifications;
using Core.Models.SuspiciousActivityReports;
using Core.ValueObjects;

namespace Core.Helpers;

public static class NotificationMessageBuilder
{
    public static NotificationMessage ForHighSeverityCase(Case @case, Translator translator, DateTimeOffset timestamp)
    {
        return new NotificationMessage
        {
            Subject = translator.Text(TranslationKeys.NotificationCaseOpenedSubject,
                translator.Severity(@case.Severity), @case.FriendlyName.ToString()),
            Body = translator.Text(TranslationKeys.NotificationCaseOpenedBody,
                @case.FriendlyName.ToString(), translator.Severity(@case.Severity), translator.Moment(@case.DueAt)),
            CaseId = @case.CaseId,
            Event = WebhookEvents.CaseCreated,
            Timestamp = timestamp
        };
    }

    public static NotificationMessage ForBreachedCase(Case @case, Translator translator, DateTimeOffset timestamp)
    {
        return new NotificationMessage
        {
            Subject = translator.Text(TranslationKeys.NotificationCaseBreachedSubject, @case.FriendlyName.ToString()),
            Body = translator.Text(TranslationKeys.NotificationCaseBreachedBody,
                @case.FriendlyName.ToString(), translator.Severity(@case.Severity), translator.Moment(@case.DueAt)),
            CaseId = @case.CaseId,
            Event = WebhookEvents.CaseBreached,
            Timestamp = timestamp
        };
    }

    public static NotificationMessage ForOverdueSuspiciousActivityReport(
        SuspiciousActivityReport suspiciousActivityReport, Translator translator, DateTimeOffset timestamp)
    {
        return new NotificationMessage
        {
            Subject = translator.Text(TranslationKeys.NotificationFilingOverdueSubject,
                suspiciousActivityReport.FriendlyName.ToString()),
            Body = translator.Text(TranslationKeys.NotificationFilingOverdueBody,
                suspiciousActivityReport.FriendlyName.ToString(),
                translator.Moment(suspiciousActivityReport.DueAt),
                translator.FilingStatus(suspiciousActivityReport.Status)),
            CaseId = suspiciousActivityReport.CaseId,
            Event = WebhookEvents.SuspiciousActivityReportOverdue,
            Timestamp = timestamp
        };
    }

    public static NotificationMessage ForDisabledWebhookConfiguration(
        DisabledReason disabledReason, Translator translator, DateTimeOffset timestamp)
    {
        return new NotificationMessage
        {
            Subject = translator.Text(TranslationKeys.NotificationWebhookDisabledSubject),
            Body = translator.Text(TranslationKeys.NotificationWebhookDisabledBody, disabledReason.ToString()),
            CaseId = null,
            Event = WebhookEvents.WebhookConfigurationDisabled,
            Timestamp = timestamp
        };
    }
}
