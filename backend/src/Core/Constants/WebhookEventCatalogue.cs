using Core.Enums;
using Core.Models.Webhooks;
using Core.ValueObjects;

namespace Core.Constants;

public static class WebhookEventCatalogue
{
    public static readonly IReadOnlyList<WebhookEventDefinition> Events =
    [
        Define(WebhookEvents.CaseCreated, WebhookEventCategory.Case,
            "A case was opened, whether by an evaluation that raised alerts or by hand."),
        Define(WebhookEvents.CaseAssigned, WebhookEventCategory.Case,
            "A case was routed to a queue or picked up by an analyst."),
        Define(WebhookEvents.CaseUnassigned, WebhookEventCategory.Case,
            "A case was released by whoever held it and is waiting to be picked up again."),
        Define(WebhookEvents.CaseStatusChanged, WebhookEventCategory.Case,
            "A case moved between statuses without being resolved."),
        Define(WebhookEvents.CaseEscalated, WebhookEventCategory.Case,
            "A case matched an escalation rule and was raised to a higher priority or a different queue."),
        Define(WebhookEvents.CaseBreached, WebhookEventCategory.Case,
            "A case passed its due time while still open."),
        Define(WebhookEvents.CaseResolved, WebhookEventCategory.Case,
            "A case was closed with a disposition."),
        Define(WebhookEvents.CaseReopened, WebhookEventCategory.Case,
            "A resolved case was reopened, which withdraws its disposition."),
        Define(WebhookEvents.DecisionResolved, WebhookEventCategory.Decision,
            "A held payment was decided, either by an analyst or by the hold expiring into its verdict."),
        Define(WebhookEvents.AlertRaised, WebhookEventCategory.Alert,
            "A rule matched and raised an alert."),
        Define(WebhookEvents.AlertSuppressed, WebhookEventCategory.Alert,
            "A rule matched but the alert was suppressed by deduplication or by a suppression window."),
        Define(WebhookEvents.ApprovalRequested, WebhookEventCategory.Approval,
            "Something that needs a second pair of eyes is waiting for one."),
        Define(WebhookEvents.ApprovalApproved, WebhookEventCategory.Approval,
            "A pending approval was granted."),
        Define(WebhookEvents.ApprovalRejected, WebhookEventCategory.Approval,
            "A pending approval was refused."),
        Define(WebhookEvents.ApprovalWithdrawn, WebhookEventCategory.Approval,
            "A pending approval was withdrawn by whoever asked for it."),
        Define(WebhookEvents.SuspiciousActivityReportOpened, WebhookEventCategory.SuspiciousActivityReport,
            "A filing was opened against a case and has a deadline from this moment."),
        Define(WebhookEvents.SuspiciousActivityReportFiled, WebhookEventCategory.SuspiciousActivityReport,
            "A filing was submitted to the reporting authority."),
        Define(WebhookEvents.SuspiciousActivityReportAcknowledged, WebhookEventCategory.SuspiciousActivityReport,
            "The reporting authority acknowledged a submitted filing."),
        Define(WebhookEvents.SuspiciousActivityReportNotFiled, WebhookEventCategory.SuspiciousActivityReport,
            "A filing was closed without being submitted, with a recorded reason."),
        Define(WebhookEvents.SuspiciousActivityReportOverdue, WebhookEventCategory.SuspiciousActivityReport,
            "A filing passed its statutory deadline without being submitted."),

        new WebhookEventDefinition
        {
            Event = new WebhookEvent(WebhookEvents.Test),
            Category = WebhookEventCategory.Diagnostic,
            Description = new EventDescription(
                "Sent only by the endpoint's test action. Delivered regardless of what the endpoint subscribes to."),
            IsSubscribable = new IsSubscribable(false),
            IntroducedInVersion = new WebhookPayloadVersion(WebhookPayloadVersions.V1)
        }
    ];

    private static WebhookEventDefinition Define(string @event, WebhookEventCategory category, string description)
    {
        return new WebhookEventDefinition
        {
            Event = new WebhookEvent(@event),
            Category = category,
            Description = new EventDescription(description),
            IsSubscribable = new IsSubscribable(true),
            IntroducedInVersion = new WebhookPayloadVersion(WebhookPayloadVersions.V1)
        };
    }
}
