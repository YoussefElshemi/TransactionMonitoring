using Core.ValueObjects;

namespace Core.Models.SuspiciousActivityReports;

public record DeclineSuspiciousActivityReportRequest
{
    public required SuspiciousActivityReportId SuspiciousActivityReportId { get; init; }
    public required Message Reason { get; init; }
    public required ApplicationUserId DecidedByUserId { get; init; }
}
