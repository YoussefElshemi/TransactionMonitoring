using Microsoft.AspNetCore.Mvc;

namespace Presentation.Models.SuspiciousActivityReports;

public record OrganizationSuspiciousActivityReportPolicyRequestDto
{
    [FromRoute]
    public required Guid OrganizationId { get; init; }
}

public record SetOrganizationSuspiciousActivityReportPolicyRequestDto : OrganizationSuspiciousActivityReportPolicyRequestDto
{
    [FromBody]
    public required SetOrganizationSuspiciousActivityReportPolicyRequestBodyDto Body { get; init; }
}

public record SetOrganizationSuspiciousActivityReportPolicyRequestBodyDto
{
    public string? ReportingEntityId { get; init; }

    public int? FilingDeadlineDays { get; init; }

    public required bool RequireFilingApproval { get; init; }
    public string? AmountPath { get; init; }
    public string? CurrencyPath { get; init; }
}

public record SuspiciousActivityReportPolicyResponseDto
{
    public required Guid OrganizationId { get; init; }
    public required string? ReportingEntityId { get; init; }
    public required int? FilingDeadlineDays { get; init; }

    public required int EffectiveFilingDeadlineDays { get; init; }
    public required bool RequireFilingApproval { get; init; }
    public required string? AmountPath { get; init; }
    public required string? CurrencyPath { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }

    public required string Mode { get; init; }
}
