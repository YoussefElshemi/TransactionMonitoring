namespace Core.Enums;

public enum CaseStatus
{
    Pending = 1,
    Assigned,
    Approved,
    Rejected,
    Expired,

    InReview,
    Escalated,
    PendingInformation,
    OnHold
}