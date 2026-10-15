using System.Diagnostics;
using Core.Constants;
using Core.Enums;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Models.RuleDeployments;
using Core.ValueObjects;
using Quartz;
using Serilog;

namespace Core.Jobs;

[DisallowConcurrentExecution]
public class RuleDeploymentJob(
    IRuleDeploymentRepository ruleDeploymentRepository,
    IRuleVersionRepository ruleVersionRepository,
    IRuleRepository ruleRepository,
    IUnitOfWork unitOfWork,
    ISchedulerFactory schedulerFactory,
    TimeProvider timeProvider,
    IModeContext modeContext,
    ILogger logger) : IJob
{
    public async ValueTask Execute(IJobExecutionContext context)
    {
        using var activity = OpenTelemetry.OpenTelemetry.MyActivitySource.StartActivity(GetType().Name);

        var ct = context.CancellationToken;
        var ruleDeploymentId = new RuleDeploymentId(Guid.Parse(context.JobDetail.Key.Name));

        try
        {
            var deployment = await ruleDeploymentRepository.GetAsync(ruleDeploymentId, ct);

            modeContext.Current = deployment.Mode;

            if (deployment.Status != RuleDeploymentStatus.InProgress)
            {
                await UnscheduleAsync(ruleDeploymentId, ct);
                return;
            }

            var nextPercentage = deployment.CurrentPercentage + deployment.StepPercentage!.Value;

            if (nextPercentage >= 100)
            {
                var targetVersion = await ruleVersionRepository.GetAsync(deployment.TargetRuleVersionId, ct);
                await ruleRepository.DeployVersionAsync(deployment.RuleId, targetVersion, ct);

                await ruleDeploymentRepository.UpdateAsync(new UpdateRuleDeploymentRequest
                {
                    RuleDeploymentId = ruleDeploymentId,
                    Status = RuleDeploymentStatus.Completed,
                    CurrentPercentage = new RolloutPercentage(100),
                    CompletedAt = new CompletedAt(timeProvider.GetUtcNow())
                }, ct);

                await unitOfWork.SaveChangesAsync(ct);
                await UnscheduleAsync(ruleDeploymentId, ct);
            }
            else
            {
                await ruleDeploymentRepository.UpdateAsync(new UpdateRuleDeploymentRequest
                {
                    RuleDeploymentId = ruleDeploymentId,
                    Status = RuleDeploymentStatus.InProgress,
                    CurrentPercentage = new RolloutPercentage(nextPercentage),
                    CompletedAt = null
                }, ct);

                await unitOfWork.SaveChangesAsync(ct);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error processing ruleDeployment {RuleDeploymentId}: {Error}", ruleDeploymentId, ex.Message);
            activity?.AddException(ex);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);

            await MarkFailedAsync(ruleDeploymentId, ct);
            await UnscheduleAsync(ruleDeploymentId, ct);
        }
    }

    private async Task MarkFailedAsync(RuleDeploymentId ruleDeploymentId, CancellationToken stoppingToken)
    {
        try
        {
            var deployment = await ruleDeploymentRepository.GetAsync(ruleDeploymentId, stoppingToken);

            if (deployment.Status != RuleDeploymentStatus.InProgress)
            {
                return;
            }

            await ruleDeploymentRepository.UpdateAsync(new UpdateRuleDeploymentRequest
            {
                RuleDeploymentId = ruleDeploymentId,
                Status = RuleDeploymentStatus.Failed,
                CurrentPercentage = deployment.CurrentPercentage,
                CompletedAt = new CompletedAt(timeProvider.GetUtcNow())
            }, stoppingToken);

            await unitOfWork.SaveChangesAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to mark ruleDeployment {RuleDeploymentId} as failed", ruleDeploymentId);
        }
    }

    private async Task UnscheduleAsync(RuleDeploymentId ruleDeploymentId, CancellationToken stoppingToken)
    {
        var scheduler = await schedulerFactory.GetScheduler(stoppingToken);
        await scheduler.DeleteJob(new JobKey(ruleDeploymentId.ToString(), QuartzConstants.RuleDeploymentGroup), stoppingToken);
    }
}
