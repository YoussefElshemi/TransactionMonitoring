using Core.Enums;
using Core.ValueObjects;

namespace Core.Models.SuspiciousActivityReports;

public record SuspiciousActivityReportTransition
{
    public required SuspiciousActivityReportId SuspiciousActivityReportId { get; init; }
    public required SuspiciousActivityReportStatus Status { get; init; }
    public required ApplicationUserId? FiledByUserId { get; init; }
    public required FilingReference? FilingReference { get; init; }
    public required FilingReference? AcknowledgementReference { get; init; }
    public required Message? NotFiledReason { get; init; }
    public required ApplicationUserId? DecidedByUserId { get; init; }
}
