using Core.ValueObjects;

namespace Core.Models.SuspiciousActivityReports;

public record OpenSuspiciousActivityReportRequest
{
    public required OrganizationId OrganizationId { get; init; }
    public required CaseId CaseId { get; init; }

    public required Narrative? Narrative { get; init; }
    public required ApplicationUserId OpenedByUserId { get; init; }
}
