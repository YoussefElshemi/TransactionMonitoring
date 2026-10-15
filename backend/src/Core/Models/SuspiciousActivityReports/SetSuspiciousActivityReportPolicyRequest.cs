using Core.ValueObjects;

namespace Core.Models.SuspiciousActivityReports;

public record SetSuspiciousActivityReportPolicyRequest
{
    public required OrganizationId OrganizationId { get; init; }
    public required ReportingEntityId? ReportingEntityId { get; init; }
    public required FilingDeadline? FilingDeadline { get; init; }
    public required IsRequired RequireFilingApproval { get; init; }
    public required PayloadFieldPath? AmountPath { get; init; }
    public required PayloadFieldPath? CurrencyPath { get; init; }
}
