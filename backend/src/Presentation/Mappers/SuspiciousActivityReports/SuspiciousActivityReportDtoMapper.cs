using Core.Configs;
using Core.Extensions;
using Core.Helpers;
using Core.Models.SuspiciousActivityReports;
using Presentation.Models.SuspiciousActivityReports;

namespace Presentation.Mappers.SuspiciousActivityReports;

public static class SuspiciousActivityReportDtoMapper
{
    public static SuspiciousActivityReportResponseDto ToDto(
        this SuspiciousActivityReport suspiciousActivityReport,
        DateTimeOffset now)
    {
        return new SuspiciousActivityReportResponseDto
        {
            Mode = suspiciousActivityReport.Mode.ToString(),
            SuspiciousActivityReportId = suspiciousActivityReport.SuspiciousActivityReportId,
            OrganizationId = suspiciousActivityReport.OrganizationId,
            CaseId = suspiciousActivityReport.CaseId,
            FriendlyName = suspiciousActivityReport.FriendlyName,
            Status = suspiciousActivityReport.Status.ToString(),
            Narrative = suspiciousActivityReport.Narrative,
            DueAt = suspiciousActivityReport.DueAt,
            IsOverdue = !suspiciousActivityReport.Status.IsTerminal() && suspiciousActivityReport.DueAt < now,
            OpenedByUserId = suspiciousActivityReport.OpenedByUserId,
            FiledByUserId = suspiciousActivityReport.FiledByUserId.HasValue ? suspiciousActivityReport.FiledByUserId.Value : null,
            FiledAt = suspiciousActivityReport.FiledAt.HasValue ? (DateTimeOffset)suspiciousActivityReport.FiledAt.Value : null,
            FilingReference = suspiciousActivityReport.FilingReference?.ToString(),
            AcknowledgedAt = suspiciousActivityReport.AcknowledgedAt.HasValue
                ? (DateTimeOffset)suspiciousActivityReport.AcknowledgedAt.Value
                : null,
            AcknowledgementReference = suspiciousActivityReport.AcknowledgementReference?.ToString(),
            NotFiledReason = suspiciousActivityReport.NotFiledReason?.ToString(),
            DecidedByUserId = suspiciousActivityReport.DecidedByUserId.HasValue ? suspiciousActivityReport.DecidedByUserId.Value : null,
            DecidedAt = suspiciousActivityReport.DecidedAt.HasValue ? (DateTimeOffset)suspiciousActivityReport.DecidedAt.Value : null,
            CreatedAt = suspiciousActivityReport.CreatedAt,
            UpdatedAt = suspiciousActivityReport.UpdatedAt
        };
    }

    public static SuspiciousActivityReportPolicyResponseDto ToDto(
        this SuspiciousActivityReportPolicy suspiciousActivityReportPolicy,
        SuspiciousActivityReportConfig suspiciousActivityReportConfig)
    {
        return new SuspiciousActivityReportPolicyResponseDto
        {
            Mode = suspiciousActivityReportPolicy.Mode.ToString(),
            OrganizationId = suspiciousActivityReportPolicy.OrganizationId,
            ReportingEntityId = suspiciousActivityReportPolicy.ReportingEntityId?.ToString(),
            FilingDeadlineDays = suspiciousActivityReportPolicy.FilingDeadline.HasValue
                ? suspiciousActivityReportPolicy.FilingDeadline.Value
                : null,
            EffectiveFilingDeadlineDays = FilingDeadlineResolver.ResolveDeadline(
                suspiciousActivityReportPolicy, suspiciousActivityReportConfig),
            RequireFilingApproval = suspiciousActivityReportPolicy.RequireFilingApproval,
            AmountPath = suspiciousActivityReportPolicy.AmountPath?.ToString(),
            CurrencyPath = suspiciousActivityReportPolicy.CurrencyPath?.ToString(),
            CreatedAt = suspiciousActivityReportPolicy.CreatedAt,
            UpdatedAt = suspiciousActivityReportPolicy.UpdatedAt
        };
    }
}
