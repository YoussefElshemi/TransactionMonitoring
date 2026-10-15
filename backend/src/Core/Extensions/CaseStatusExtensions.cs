using Core.Enums;

namespace Core.Extensions;

public static class CaseStatusExtensions
{
    private static readonly CaseStatus[] TerminalStatuses =
    [
        CaseStatus.Approved,
        CaseStatus.Rejected,
        CaseStatus.Expired
    ];

    public static readonly int[] TerminalStatusIds = [..TerminalStatuses.Select(x => (int)x)];

    private static readonly Dictionary<CaseStatus, CaseStatus[]> AllowedTransitions = new()
    {
        [CaseStatus.Assigned] = [CaseStatus.InReview, CaseStatus.Escalated, CaseStatus.PendingInformation, CaseStatus.OnHold],
        [CaseStatus.InReview] = [CaseStatus.Escalated, CaseStatus.PendingInformation, CaseStatus.OnHold],
        [CaseStatus.Escalated] = [CaseStatus.InReview, CaseStatus.PendingInformation, CaseStatus.OnHold],
        [CaseStatus.PendingInformation] = [CaseStatus.InReview, CaseStatus.Escalated, CaseStatus.OnHold],
        [CaseStatus.OnHold] = [CaseStatus.InReview, CaseStatus.Escalated, CaseStatus.PendingInformation]
    };

    public static bool IsTerminal(this CaseStatus caseStatus)
    {
        return TerminalStatuses.Contains(caseStatus);
    }

    public static bool CanTransitionTo(this CaseStatus from, CaseStatus to)
    {
        return AllowedTransitions.TryGetValue(from, out var allowed) && allowed.Contains(to);
    }

    public static IReadOnlyCollection<CaseStatus> AllowedTransitionsFrom(this CaseStatus from)
    {
        return AllowedTransitions.TryGetValue(from, out var allowed) ? allowed : [];
    }
}
