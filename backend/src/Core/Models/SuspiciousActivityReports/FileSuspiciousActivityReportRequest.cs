using Core.ValueObjects;

namespace Core.Models.SuspiciousActivityReports;

public record FileSuspiciousActivityReportRequest
{
    public required SuspiciousActivityReportId SuspiciousActivityReportId { get; init; }

    public required FilingReference FilingReference { get; init; }
    public required ApplicationUserId FiledByUserId { get; init; }
}
