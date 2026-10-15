using Core.Configs;
using Core.Models.SuspiciousActivityReports;
using Core.ValueObjects;

namespace Core.Helpers;

public static class FilingDeadlineResolver
{
    public static DueAt Resolve(
        SuspiciousActivityReportPolicy? suspiciousActivityReportPolicy,
        SuspiciousActivityReportConfig suspiciousActivityReportConfig,
        CreatedAt caseOpenedAt)
    {
        var filingDeadline = ResolveDeadline(suspiciousActivityReportPolicy, suspiciousActivityReportConfig);

        return new DueAt((DateTimeOffset)caseOpenedAt + filingDeadline.ToTimeSpan());
    }

    public static FilingDeadline ResolveDeadline(
        SuspiciousActivityReportPolicy? suspiciousActivityReportPolicy,
        SuspiciousActivityReportConfig suspiciousActivityReportConfig)
    {
        return suspiciousActivityReportPolicy?.FilingDeadline ??
               new FilingDeadline(suspiciousActivityReportConfig.DefaultFilingDeadlineDays);
    }

    public static bool RequiresFilingApproval(SuspiciousActivityReportPolicy? suspiciousActivityReportPolicy)
    {
        return suspiciousActivityReportPolicy is not null && suspiciousActivityReportPolicy.RequireFilingApproval;
    }
}
