using Core.Enums;
using Core.ValueObjects;

namespace Core.Models.Statistics;

public record CaseStatistic
{
    public required TotalCount Total { get; init; }

    public required IReadOnlyDictionary<CaseStatus, TotalCount> ByStatus { get; init; }
    public required TimeSpan? AverageResolutionTime { get; init; }

    public required TotalCount Breached { get; init; }
    public required BreachRate BreachRate { get; init; }
}
