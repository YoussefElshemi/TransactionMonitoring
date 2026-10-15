namespace Core.Models.Statistics;

public record Statistic
{
    public required CaseStatistic Case { get; init; }
    public required Dictionary<DateOnly, int> Usage { get; init; }
}