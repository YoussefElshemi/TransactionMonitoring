using Core.Models.Statistics;
using Core.ValueObjects;
using Presentation.Models.Statistics;

namespace Presentation.Mappers.Statistics;

public static class GetStatisticDtoMapper
{
    public static StatisticRequest ToModel(this StatisticRequestDto statisticRequestDto)
    {
        return new StatisticRequest
        {
            OrganizationId = new OrganizationId(statisticRequestDto.OrganizationId),
            FromDate = new DateRange(statisticRequestDto.FromDate),
            ToDate = new DateRange(statisticRequestDto.ToDate)
        };
    }
}