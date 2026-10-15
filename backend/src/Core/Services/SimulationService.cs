using Core.Constants;
using Core.Enums;
using Core.Exceptions;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Jobs;
using Core.Models.Common;
using Core.Models.Simulations;
using Core.ValueObjects;
using Quartz;

namespace Core.Services;

public class SimulationService(
    ISimulationRepository simulationRepository,
    IUnitOfWork unitOfWork,
    ISchedulerFactory schedulerFactory,
    ISandboxCreditService sandboxCreditService,
    IRuleSetRepository ruleSetRepository,
    TimeProvider timeProvider,
    IModeContext modeContext) : ISimulationService
{
    public async Task<Simulation> StartAsync(StartSimulationRequest startSimulationRequest, CancellationToken stoppingToken)
    {
        if (modeContext.CurrentOrLive != Mode.Test)
        {
            throw new NotAvailableInLiveModeException("Running the payment simulator");
        }

        await sandboxCreditService.EnsureAvailableAsync(
            startSimulationRequest.OrganizationId,
            await CostAsync(startSimulationRequest, stoppingToken),
            stoppingToken);

        var now = timeProvider.GetUtcNow();

        var simulation = new Simulation
        {
            Mode = Mode.Test,
            SimulationId = new SimulationId(Guid.NewGuid()),
            OrganizationId = startSimulationRequest.OrganizationId,
            Scenario = startSimulationRequest.Scenario,
            Status = SimulationStatus.Pending,
            TotalPayments = startSimulationRequest.PaymentCount,
            ProcessedCount = new ProcessedCount(0),

            Seed = startSimulationRequest.Seed ?? new SimulationSeed(Random.Shared.Next(1, int.MaxValue)),
            CreatedByUserId = startSimulationRequest.CreatedByUserId,
            CompletedAt = null,
            FailureReason = null,
            CreatedAt = new CreatedAt(now),
            UpdatedAt = new UpdatedAt(now)
        };

        await simulationRepository.AddAsync(simulation, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);

        try
        {
            await ScheduleAsync(simulation.SimulationId, stoppingToken);
        }
        catch (Exception exception)
        {
            await UpdateAsync(new UpdateSimulationRequest
            {
                SimulationId = simulation.SimulationId,
                Status = SimulationStatus.Failed,
                ProcessedCount = new ProcessedCount(0),
                CompletedAt = new CompletedAt(timeProvider.GetUtcNow()),
                FailureReason = new FailureReason(exception.Message)
            }, stoppingToken);

            throw;
        }

        return await GetAsync(simulation.SimulationId, stoppingToken);
    }

    public Task<Simulation> GetAsync(SimulationId simulationId, CancellationToken stoppingToken)
    {
        return simulationRepository.GetAsync(simulationId, stoppingToken);
    }

    public Task<Exists> ExistsAsync(OrganizationId organizationId, SimulationId simulationId, CancellationToken stoppingToken)
    {
        return simulationRepository.ExistsAsync(organizationId, simulationId, stoppingToken);
    }

    public Task<Exists> HasRunningAsync(OrganizationId organizationId, CancellationToken stoppingToken)
    {
        return simulationRepository.HasRunningAsync(organizationId, stoppingToken);
    }

    public async Task UpdateAsync(UpdateSimulationRequest updateSimulationRequest, CancellationToken stoppingToken)
    {
        await simulationRepository.UpdateAsync(updateSimulationRequest, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);
    }

    public Task<PagedResponse<Simulation>> ListAsync(ListSimulationRequest listSimulationRequest, CancellationToken stoppingToken)
    {
        return simulationRepository.ListAsync(listSimulationRequest, stoppingToken);
    }

    public Task<TotalCount> GetCountAsync(ListSimulationRequest listSimulationRequest, CancellationToken stoppingToken)
    {
        return simulationRepository.GetCountAsync(listSimulationRequest, stoppingToken);
    }

    private async Task<Count> CostAsync(StartSimulationRequest startSimulationRequest, CancellationToken stoppingToken)
    {
        var ruleSets = await ruleSetRepository.GetAllAsync(startSimulationRequest.OrganizationId, stoppingToken);

        var ruleCount = ruleSets
            .Where(x => x.IsEnabled)
            .SelectMany(x => x.Rules)
            .Count(x => x.IsEnabled && !x.IsDeleted);

        return new Count(startSimulationRequest.PaymentCount * Math.Max(1, ruleCount));
    }

    private async Task ScheduleAsync(SimulationId simulationId, CancellationToken stoppingToken)
    {
        var job = JobBuilder.Create<SimulationJob>()
            .WithIdentity(simulationId.ToString(), QuartzConstants.SimulationGroup)
            .StoreDurably()
            .RequestRecovery()
            .Build();

        var trigger = TriggerBuilder.Create()
            .ForJob(job)
            .WithIdentity(simulationId.ToString(), QuartzConstants.SimulationGroup)
            .StartNow()
            .Build();

        var scheduler = await schedulerFactory.GetScheduler(stoppingToken);
        await scheduler.ScheduleJob(job, trigger, stoppingToken);
    }
}
