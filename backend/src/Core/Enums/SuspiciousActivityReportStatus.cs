namespace Core.Enums;

public enum SuspiciousActivityReportStatus
{
    Draft = 1,

    PendingApproval,
    Approved,
    Filed,

    Acknowledged,

    NotFiled
}
