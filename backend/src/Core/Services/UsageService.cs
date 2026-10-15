using Core.Enums;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Mappers;
using Core.Models.Common;
using Core.Models.Statistics;
using Core.Models.Usages;
using Core.ValueObjects;

namespace Core.Services;

public class UsageService(
    IUsageRepository usageRepository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IModeContext modeContext) : IUsageService
{
    public async Task<Usage> CreateAsync(CreateUsageRequest createUsageRequest, CancellationToken stoppingToken)
    {
        if (modeContext.CurrentOrLive == Mode.Test && createUsageRequest.Count <= 0)
        {
            createUsageRequest = createUsageRequest with { Count = new Count(1) };
        }

        var usage = createUsageRequest.ToModel(timeProvider, modeContext.CurrentOrLive);

        await usageRepository.AddAsync(usage, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);

        return await GetAsync(usage.UsageId, stoppingToken);
    }

    public async Task RecordBillableAsync(CreateUsageRequest createUsageRequest, CancellationToken stoppingToken)
    {
        // The sandbox ledger row is written when the credit is reserved; writing one here would charge twice.
        if (modeContext.CurrentOrLive == Mode.Test)
        {
            return;
        }

        await CreateAsync(createUsageRequest, stoppingToken);
    }

    public Task<Usage> GetAsync(UsageId usageId, CancellationToken stoppingToken)
    {
        return usageRepository.GetAsync(usageId, stoppingToken);
    }

    public Task<Exists> ExistsAsync(OrganizationId organizationId, UsageId usageId, CancellationToken stoppingToken)
    {
        return usageRepository.ExistsAsync(organizationId, usageId, stoppingToken);
    }

    public Task<PagedResponse<Usage>> ListAsync(ListUsageRequest listUsageRequest, CancellationToken stoppingToken)
    {
        return usageRepository.ListAsync(listUsageRequest, stoppingToken);
    }

    public Task<TotalCount> GetCountAsync(ListUsageRequest listUsageRequest, CancellationToken stoppingToken)
    {
        return usageRepository.GetCountAsync(listUsageRequest, stoppingToken);
    }

    public Task<Sum> SumByDateAsync(SumUsageByDateRequest sumUsageByDateRequest, CancellationToken stoppingToken)
    {
        var daysInMonth = DateTime.DaysInMonth(sumUsageByDateRequest.Year, sumUsageByDateRequest.Month);
        var fromDate = new DateRange(new DateOnly(sumUsageByDateRequest.Year, sumUsageByDateRequest.Month, 1));
        var toDate = new DateRange(new DateOnly(sumUsageByDateRequest.Year, sumUsageByDateRequest.Month, daysInMonth));

        var listUsageRequest = new ListUsageRequest
        {
            OrganizationId = sumUsageByDateRequest.OrganizationId,
            FromDate = fromDate,
            ToDate = toDate,
            PageNumber = default,
            PageSize = default,
            Resource = null
        };

        return usageRepository.GetSumAsync(listUsageRequest, stoppingToken);
    }

    public Task<Dictionary<BillableResource, Count>> GroupByResourceAsync(SumUsageByDateRequest sumUsageByDateRequest, CancellationToken stoppingToken)
    {
        var daysInMonth = DateTime.DaysInMonth(sumUsageByDateRequest.Year, sumUsageByDateRequest.Month);

        var listUsageRequest = new ListUsageRequest
        {
            OrganizationId = sumUsageByDateRequest.OrganizationId,
            FromDate = new DateRange(new DateOnly(sumUsageByDateRequest.Year, sumUsageByDateRequest.Month, 1)),
            ToDate = new DateRange(new DateOnly(sumUsageByDateRequest.Year, sumUsageByDateRequest.Month, daysInMonth)),
            PageNumber = default,
            PageSize = default,
            Resource = null
        };

        return usageRepository.GroupByResourceAsync(listUsageRequest, stoppingToken);
    }

    public Task<Dictionary<DateOnly, int>> GroupByDateAsync(StatisticRequest statisticRequest, CancellationToken stoppingToken)
    {
        var listUsageRequest = new ListUsageRequest
        {
            OrganizationId = statisticRequest.OrganizationId,
            FromDate = statisticRequest.FromDate,
            ToDate = statisticRequest.ToDate,
            PageNumber = default,
            PageSize = default,
            Resource = null
        };

        return usageRepository.GroupByDateAsync(listUsageRequest, stoppingToken);
    }
}