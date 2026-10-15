namespace Presentation.Models.Statistics;

public class StatisticResponseDto
{
    public required CaseStatisticResponseDto Case { get; init; }
    public required Dictionary<string, int> Usage { get; init; }
}