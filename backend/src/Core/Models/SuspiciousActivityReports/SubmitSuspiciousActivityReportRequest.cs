using Core.ValueObjects;

namespace Core.Models.SuspiciousActivityReports;

public record SubmitSuspiciousActivityReportRequest
{
    public required SuspiciousActivityReportId SuspiciousActivityReportId { get; init; }
    public required Message? Reason { get; init; }
    public required ApplicationUserId SubmittedByUserId { get; init; }
}
