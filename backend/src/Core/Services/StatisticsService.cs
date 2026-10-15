using Core.Enums;
using Core.Interfaces.Services;
using Core.Models.Cases;
using Core.Models.Statistics;
using Core.ValueObjects;

namespace Core.Services;

public class StatisticsService(ICaseService caseService, IUsageService usageService) : IStatisticsService
{
    public async Task<Statistic> GetStatisticsAsync(StatisticRequest statisticRequest, CancellationToken stoppingToken)
    {
        var caseCountsByStatus = await GetCaseCountsByStatus(statisticRequest, stoppingToken);

        var totalCaseCount = new TotalCount(caseCountsByStatus.Values.Sum(x => x));
        var averageCaseResolutionTime = await caseService.GetAverageResolutionTimeAsync(statisticRequest, stoppingToken);
        var breachedCaseCount = await caseService.CountBreachedAsync(ToListCaseRequest(statisticRequest), stoppingToken);
        var usage = await usageService.GroupByDateAsync(statisticRequest, stoppingToken);

        return new Statistic
        {
            Case = new CaseStatistic
            {
                Total = totalCaseCount,
                ByStatus = caseCountsByStatus,
                AverageResolutionTime = averageCaseResolutionTime,
                Breached = breachedCaseCount,
                BreachRate = ToBreachRate(breachedCaseCount, totalCaseCount)
            },
            Usage = usage
        };
    }

    private static BreachRate ToBreachRate(TotalCount breached, TotalCount total)
    {
        return total == 0
            ? new BreachRate(0)
            : new BreachRate(Math.Round((decimal)(int)breached * 100 / (int)total, 2));
    }

    private Task<Dictionary<CaseStatus, TotalCount>> GetCaseCountsByStatus(StatisticRequest statisticRequest, CancellationToken stoppingToken)
    {
        return caseService.CountByStatusAsync(ToListCaseRequest(statisticRequest), stoppingToken);
    }

    private static ListCaseRequest ToListCaseRequest(StatisticRequest statisticRequest)
    {
        return new ListCaseRequest
        {
            OrganizationId = statisticRequest.OrganizationId,
            Status = null,
            FromDate = statisticRequest.FromDate,
            ToDate = statisticRequest.ToDate,
            FriendlyName = null,
            Disposition = null,
            Severity = null,
            AssignedToUserId = null,
            QueueId = null,
            Unrouted = null,
            QueueMemberUserId = null,
            Breached = null,
            DueBefore = null,
            Escalated = null,
            PartyId = null,
            Query = null,
            PageNumber = default,
            PageSize = default
        };
    }
}