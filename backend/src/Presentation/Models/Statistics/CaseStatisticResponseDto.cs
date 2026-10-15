namespace Presentation.Models.Statistics;

public class CaseStatisticResponseDto
{
    public required int Total { get; init; }
    public required IReadOnlyDictionary<string, int> ByStatus { get; init; }
    public required double? AverageResolutionTime { get; init; }
    public required int Breached { get; init; }

    public required decimal BreachRate { get; init; }
}
