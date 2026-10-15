using System.Diagnostics;
using Core.Configs;
using Core.Constants;
using Core.Enums;
using Core.Helpers;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Models.RateLimits;
using Core.Models.RuleSets;
using Core.Models.Simulations;
using Core.ValueObjects;
using Microsoft.Extensions.Options;
using Quartz;
using Serilog;

namespace Core.Jobs;

[DisallowConcurrentExecution]
public class SimulationJob(
    ISimulationRepository simulationRepository,
    ITransactionSchemaRepository transactionSchemaRepository,
    IPartyMappingRepository partyMappingRepository,
    IRuleSetService ruleSetService,
    IDistributedRateLimiter distributedRateLimiter,
    IUnitOfWork unitOfWork,
    ISchedulerFactory schedulerFactory,
    IModeContext modeContext,
    IOptions<AppConfig> appConfig,
    TimeProvider timeProvider,
    ILogger logger) : IJob
{
    public async ValueTask Execute(IJobExecutionContext context)
    {
        using var activity = OpenTelemetry.OpenTelemetry.MyActivitySource.StartActivity(GetType().Name);

        var ct = context.CancellationToken;
        var simulationId = new SimulationId(Guid.Parse(context.JobDetail.Key.Name));
        var simulation = await simulationRepository.GetAsync(simulationId, ct);

        modeContext.Current = simulation.Mode;

        if (simulation.Status is SimulationStatus.Completed or SimulationStatus.Failed)
        {
            return;
        }

        activity?.SetTag("simulation.scenario", simulation.Scenario.ToString());
        activity?.SetTag("simulation.payments", (int)simulation.TotalPayments);

        var processedCount = simulation.ProcessedCount;

        try
        {
            var payments = SimulatedPaymentFactory.Create(new SimulationPlan
            {
                SimulationId = simulation.SimulationId,
                Scenario = simulation.Scenario,
                PaymentCount = simulation.TotalPayments,
                Seed = simulation.Seed,
                TransactionSchema = await transactionSchemaRepository.FindAsync(simulation.OrganizationId, new IncludeDeleted(false), ct),
                PartyMapping = await partyMappingRepository.FindAsync(simulation.OrganizationId, new IncludeDeleted(false), ct),
                GeneratedAt = new EvaluatedAt(timeProvider.GetUtcNow())
            });

            await UpdateAsync(simulationId, SimulationStatus.Running, processedCount, null, null, ct);

            var chunkSize = Math.Max(1, appConfig.Value.BackgroundServiceConfig.SimulationChunkSize);

            foreach (var payment in payments.Skip(processedCount))
            {
                ct.ThrowIfCancellationRequested();

                await AwaitEvaluationBudgetAsync(simulation.OrganizationId, simulation.Mode, ct);

                await ruleSetService.EvaluateAllAsync(new EvaluateRuleSetsRequest
                {
                    OrganizationId = simulation.OrganizationId,
                    Payload = payment.Payload,
                    ExternalReference = payment.ExternalReference,
                    CreateCase = new CreateCase(true)
                }, ct);

                processedCount = new ProcessedCount(processedCount + 1);

                if (processedCount % chunkSize == 0)
                {
                    await UpdateAsync(simulationId, SimulationStatus.Running, processedCount, null, null, ct);
                }
            }

            await UpdateAsync(simulationId, SimulationStatus.Completed, processedCount, new CompletedAt(timeProvider.GetUtcNow()), null, ct);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.Error(exception, "Error running simulation {SimulationId}: {Error}", simulationId, exception.Message);
            activity?.AddException(exception);
            activity?.SetStatus(ActivityStatusCode.Error, exception.Message);

            await UpdateAsync(
                simulationId,
                SimulationStatus.Failed,
                processedCount,
                new CompletedAt(timeProvider.GetUtcNow()),
                new FailureReason(exception.Message),
                CancellationToken.None);
        }

        await UnscheduleAsync(simulationId, CancellationToken.None);
    }

    private async Task AwaitEvaluationBudgetAsync(OrganizationId organizationId, Mode mode, CancellationToken stoppingToken)
    {
        var rateLimitConfig = appConfig.Value.RateLimitConfig;

        if (!rateLimitConfig.Enabled)
        {
            return;
        }

        var bucketConfig = rateLimitConfig.SandboxPools.TryGetValue(RateLimitPool.Evaluation, out var configured)
            ? configured
            : rateLimitConfig.FallbackBucket;

        var pace = TimeSpan.FromSeconds(
            (double)bucketConfig.ReplenishmentPeriodSeconds / Math.Max(bucketConfig.TokensPerPeriod, 1));

        if (!rateLimitConfig.Distributed.Enabled)
        {
            await Task.Delay(pace, stoppingToken);

            return;
        }

        var bucket = new RateLimitBucket
        {
            Key = new RateLimitKey(RateLimitKeyHelper.ForOrganization(
                rateLimitConfig.Distributed.KeyPrefix, organizationId, mode, RateLimitPool.Evaluation)),
            PermitLimit = new PermitLimit(bucketConfig.PermitLimit),
            TokensPerPeriod = new TokensPerPeriod(bucketConfig.TokensPerPeriod),
            ReplenishmentPeriod = new ReplenishmentPeriod(bucketConfig.ReplenishmentPeriodSeconds)
        };

        while (true)
        {
            var decision = await distributedRateLimiter.AcquireAsync(
                new RateLimitAcquireRequest { Buckets = [bucket] }, stoppingToken);

            if (decision.Outcome == RateLimitOutcome.Allowed)
            {
                return;
            }

            if (decision.Outcome == RateLimitOutcome.Unavailable)
            {
                await Task.Delay(pace, stoppingToken);

                return;
            }

            await Task.Delay(TimeSpan.FromSeconds(Math.Max(decision.RetryAfter!.Value, 1)), stoppingToken);
        }
    }

    private async Task UpdateAsync(
        SimulationId simulationId,
        SimulationStatus status,
        ProcessedCount processedCount,
        CompletedAt? completedAt,
        FailureReason? failureReason,
        CancellationToken stoppingToken)
    {
        await simulationRepository.UpdateAsync(new UpdateSimulationRequest
        {
            SimulationId = simulationId,
            Status = status,
            ProcessedCount = processedCount,
            CompletedAt = completedAt,
            FailureReason = failureReason
        }, stoppingToken);

        await unitOfWork.SaveChangesAsync(stoppingToken);
    }

    private async Task UnscheduleAsync(SimulationId simulationId, CancellationToken stoppingToken)
    {
        var scheduler = await schedulerFactory.GetScheduler(stoppingToken);
        await scheduler.DeleteJob(new JobKey(simulationId.ToString(), QuartzConstants.SimulationGroup), stoppingToken);
    }
}
