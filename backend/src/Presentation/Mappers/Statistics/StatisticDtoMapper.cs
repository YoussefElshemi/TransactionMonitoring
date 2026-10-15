using Core.Constants;
using Core.Models.Statistics;
using Presentation.Models.Statistics;

namespace Presentation.Mappers.Statistics;

public static class StatisticDtoMapper
{
    public static StatisticResponseDto ToDto(this Statistic statistic)
    {
        return new StatisticResponseDto
        {
            Case = new CaseStatisticResponseDto
            {
                Total = statistic.Case.Total,
                ByStatus = statistic.Case.ByStatus.ToDictionary(x => x.Key.ToString(), x => (int)x.Value),
                AverageResolutionTime = statistic.Case.AverageResolutionTime?.TotalMilliseconds,
                Breached = statistic.Case.Breached,
                BreachRate = statistic.Case.BreachRate
            },
            Usage = statistic.Usage.ToDictionary(x => x.Key.ToString(DateTimeConstants.DateFormat), x => x.Value)
        };
    }
}