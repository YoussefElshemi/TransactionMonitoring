using Core.ValueObjects;

namespace Core.Models.SuspiciousActivityReports;

public record UpdateSuspiciousActivityReportRequest
{
    public required SuspiciousActivityReportId SuspiciousActivityReportId { get; init; }
    public required Narrative Narrative { get; init; }
}
