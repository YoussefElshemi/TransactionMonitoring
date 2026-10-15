using Core.ValueObjects;

namespace Core.Models.SuspiciousActivityReports;

public record AcknowledgeSuspiciousActivityReportRequest
{
    public required SuspiciousActivityReportId SuspiciousActivityReportId { get; init; }
    public required FilingReference? AcknowledgementReference { get; init; }
}
