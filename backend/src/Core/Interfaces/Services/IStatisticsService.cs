using Core.Models.Statistics;

namespace Core.Interfaces.Services;

public interface IStatisticsService
{
    Task<Statistic> GetStatisticsAsync(StatisticRequest statisticRequest, CancellationToken stoppingToken);
}