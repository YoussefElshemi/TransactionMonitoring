using Core.Enums;
using Core.Models.Common;
using Core.ValueObjects;

namespace Core.Models.SuspiciousActivityReports;

public record ListSuspiciousActivityReportRequest : PagedRequest
{
    public required OrganizationId OrganizationId { get; init; }
    public required SuspiciousActivityReportStatus? Status { get; init; }
    public required CaseId? CaseId { get; init; }

    public required IsBreached? IsOverdue { get; init; }
}
