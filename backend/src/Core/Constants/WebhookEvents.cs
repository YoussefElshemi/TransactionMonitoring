using System.Collections.Frozen;

namespace Core.Constants;

public static class WebhookEvents
{
    public const string Test = "test";
    public const string CaseCreated = "case.created";
    public const string CaseAssigned = "case.assigned";
    public const string CaseUnassigned = "case.unassigned";
    public const string CaseResolved = "case.resolved";
    public const string CaseStatusChanged = "case.status_changed";
    public const string CaseReopened = "case.reopened";
    public const string CaseEscalated = "case.escalated";
    public const string CaseBreached = "case.breached";
    public const string DecisionResolved = "decision.resolved";
    public const string AlertRaised = "alert.raised";
    public const string AlertSuppressed = "alert.suppressed";
    public const string ApprovalRequested = "approval.requested";
    public const string ApprovalApproved = "approval.approved";
    public const string ApprovalRejected = "approval.rejected";
    public const string ApprovalWithdrawn = "approval.withdrawn";
    public const string SuspiciousActivityReportOpened = "suspicious_activity_report.opened";
    public const string SuspiciousActivityReportFiled = "suspicious_activity_report.filed";
    public const string SuspiciousActivityReportAcknowledged = "suspicious_activity_report.acknowledged";
    public const string SuspiciousActivityReportNotFiled = "suspicious_activity_report.not_filed";
    public const string SuspiciousActivityReportOverdue = "suspicious_activity_report.overdue";

    public const string IncidentUpdated = "incident.updated";
    public const string MaintenanceScheduled = "maintenance.scheduled";
    public const string WebhookConfigurationDisabled = "webhook_configuration.disabled";

    public const string InvitationSent = "invitation.sent";

    public const string InvoiceOverdue = "invoice.overdue";

    public static readonly FrozenSet<string> Subscribable = new[]
    {
        CaseCreated,
        CaseAssigned,
        CaseUnassigned,
        CaseResolved,
        CaseStatusChanged,
        CaseReopened,
        CaseEscalated,
        CaseBreached,
        DecisionResolved,
        AlertRaised,
        AlertSuppressed,
        ApprovalRequested,
        ApprovalApproved,
        ApprovalRejected,
        ApprovalWithdrawn,
        SuspiciousActivityReportOpened,
        SuspiciousActivityReportFiled,
        SuspiciousActivityReportAcknowledged,
        SuspiciousActivityReportNotFiled,
        SuspiciousActivityReportOverdue
    }.ToFrozenSet();

    public static readonly FrozenSet<string> All = Subscribable.Append(Test).ToFrozenSet();
}
