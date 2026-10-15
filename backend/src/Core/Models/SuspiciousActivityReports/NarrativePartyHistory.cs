using Core.Models.Parties;
using Core.ValueObjects;

namespace Core.Models.SuspiciousActivityReports;

public record NarrativePartyHistory
{
    public required Party Party { get; init; }
    public required TotalCount CaseCount { get; init; }
    public required TotalCount TransactionCount { get; init; }
}
