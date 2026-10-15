using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Jobs;
using Core.Mappers;
using Core.Models.BackTests;
using Core.Models.Common;
using Core.ValueObjects;
using Quartz;

namespace Core.Services;

public class BackTestService(
    IBackTestRepository backTestRepository,
    IRuleRepository ruleRepository,
    IRuleVersionRepository ruleVersionRepository,
    IUnitOfWork unitOfWork,
    ISchedulerFactory schedulerFactory,
    TimeProvider timeProvider,
    IModeContext modeContext) : IBackTestService
{
    public async Task<BackTest> CreateAsync(CreateBackTestRequest request, CancellationToken stoppingToken)
    {
        var rule = await ruleRepository.GetAsync(request.RuleId, stoppingToken);
        var versionNumber = request.VersionNumber ?? rule.CurrentVersionNumber;
        var targetVersion = await ruleVersionRepository.GetByVersionNumberAsync(request.RuleId, versionNumber, stoppingToken);

        var backTest = request.ToModel(targetVersion.RuleVersionId, timeProvider, modeContext.CurrentOrLive);

        await backTestRepository.AddAsync(backTest, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);

        var job = JobBuilder.Create<BackTestJob>()
            .WithIdentity(backTest.BackTestId.ToString(), "BackTests")
            .StoreDurably()
            .RequestRecovery()
            .Build();

        var trigger = TriggerBuilder.Create()
            .ForJob(job)
            .WithIdentity(backTest.BackTestId.ToString(), "BackTests")
            .StartNow()
            .Build();

        var scheduler = await schedulerFactory.GetScheduler(stoppingToken);
        await scheduler.ScheduleJob(job, trigger, stoppingToken);

        return await GetAsync(backTest.BackTestId, stoppingToken);
    }

    public Task<BackTest> GetAsync(BackTestId backTestId, CancellationToken stoppingToken)
    {
        return backTestRepository.GetAsync(backTestId, stoppingToken);
    }

    public Task<Exists> ExistsAsync(OrganizationId organizationId, BackTestId backTestId, CancellationToken stoppingToken)
    {
        return backTestRepository.ExistsAsync(organizationId, backTestId, stoppingToken);
    }

    public Task<Exists> ExistsActiveAsync(OrganizationId organizationId, RuleId ruleId, CancellationToken stoppingToken)
    {
        return backTestRepository.ExistsActiveAsync(organizationId, ruleId, stoppingToken);
    }

    public Task<PagedResponse<BackTest>> ListAsync(ListBackTestRequest request, CancellationToken stoppingToken)
    {
        return backTestRepository.ListAsync(request, stoppingToken);
    }

    public Task<TotalCount> GetCountAsync(ListBackTestRequest request, CancellationToken stoppingToken)
    {
        return backTestRepository.GetCountAsync(request, stoppingToken);
    }
}
