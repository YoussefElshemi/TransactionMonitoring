using Core.Enums;
using Core.Models.SuspiciousActivityReports;
using Core.ValueObjects;
using Infrastructure.Entities;

namespace Infrastructure.Mappers;

public static class SuspiciousActivityReportPolicyMapper
{
    public static SuspiciousActivityReportPolicy ToModel(this SuspiciousActivityReportPolicyEntity suspiciousActivityReportPolicyEntity)
    {
        return new SuspiciousActivityReportPolicy
        {
            Mode = (Mode)suspiciousActivityReportPolicyEntity.ModeId,
            OrganizationId = new OrganizationId(suspiciousActivityReportPolicyEntity.OrganizationId),
            ReportingEntityId = !string.IsNullOrWhiteSpace(suspiciousActivityReportPolicyEntity.ReportingEntityId)
                ? new ReportingEntityId(suspiciousActivityReportPolicyEntity.ReportingEntityId)
                : null,
            FilingDeadline = suspiciousActivityReportPolicyEntity.FilingDeadlineDays.HasValue
                ? new FilingDeadline(suspiciousActivityReportPolicyEntity.FilingDeadlineDays.Value)
                : null,
            RequireFilingApproval = new IsRequired(suspiciousActivityReportPolicyEntity.RequireFilingApproval),
            AmountPath = !string.IsNullOrWhiteSpace(suspiciousActivityReportPolicyEntity.AmountPath)
                ? new PayloadFieldPath(suspiciousActivityReportPolicyEntity.AmountPath)
                : null,
            CurrencyPath = !string.IsNullOrWhiteSpace(suspiciousActivityReportPolicyEntity.CurrencyPath)
                ? new PayloadFieldPath(suspiciousActivityReportPolicyEntity.CurrencyPath)
                : null,
            CreatedAt = new CreatedAt(suspiciousActivityReportPolicyEntity.CreatedAt),
            UpdatedAt = new UpdatedAt(suspiciousActivityReportPolicyEntity.UpdatedAt)
        };
    }
}
