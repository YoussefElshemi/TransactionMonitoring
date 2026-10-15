using Core.Enums;
using Core.Models.BaseModels;
using Core.Models.Users;
using Core.ValueObjects;

namespace Core.Models.SuspiciousActivityReports;

public record SuspiciousActivityReport : BaseModel
{
    public required SuspiciousActivityReportId SuspiciousActivityReportId { get; init; }
    public required OrganizationId OrganizationId { get; init; }
    public required CaseId CaseId { get; init; }
    public required FriendlyName FriendlyName { get; init; }
    public required SuspiciousActivityReportStatus Status { get; init; }

    public required Narrative Narrative { get; init; }

    public required DueAt DueAt { get; init; }

    public required OverdueNotifiedAt? OverdueNotifiedAt { get; init; }

    public required ApplicationUserId OpenedByUserId { get; init; }
    public required ApplicationUser? OpenedByUser { get; init; }

    public required ApplicationUserId? FiledByUserId { get; init; }
    public required ApplicationUser? FiledByUser { get; init; }
    public required FiledAt? FiledAt { get; init; }
    public required FilingReference? FilingReference { get; init; }

    public required AcknowledgedAt? AcknowledgedAt { get; init; }
    public required FilingReference? AcknowledgementReference { get; init; }

    public required Message? NotFiledReason { get; init; }
    public required ApplicationUserId? DecidedByUserId { get; init; }
    public required ApplicationUser? DecidedByUser { get; init; }

    public required DecidedAt? DecidedAt { get; init; }

    public required Mode Mode { get; init; }
}
