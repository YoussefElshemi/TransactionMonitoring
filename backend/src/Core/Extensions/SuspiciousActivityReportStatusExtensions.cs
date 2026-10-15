using Core.Enums;

namespace Core.Extensions;

public static class SuspiciousActivityReportStatusExtensions
{
    private static readonly SuspiciousActivityReportStatus[] TerminalStatuses =
    [
        SuspiciousActivityReportStatus.Filed,
        SuspiciousActivityReportStatus.Acknowledged,
        SuspiciousActivityReportStatus.NotFiled
    ];

    public static readonly int[] TerminalStatusIds = [..TerminalStatuses.Select(x => (int)x)];

    public static bool IsTerminal(this SuspiciousActivityReportStatus suspiciousActivityReportStatus)
    {
        return TerminalStatuses.Contains(suspiciousActivityReportStatus);
    }

    public static bool IsEditable(this SuspiciousActivityReportStatus suspiciousActivityReportStatus)
    {
        return suspiciousActivityReportStatus == SuspiciousActivityReportStatus.Draft;
    }
}
