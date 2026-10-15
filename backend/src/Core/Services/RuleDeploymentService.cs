using System.Net;
using Core.Constants;
using Core.Enums;
using Core.Helpers;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Jobs;
using Core.Mappers;
using Core.Mappers.Webhooks;
using Core.Models.Approvals;
using Core.Models.Common;
using Core.Models.RuleDeployments;
using Core.Models.Rules;
using Core.ValueObjects;
using FluentValidation;
using FluentValidation.Results;
using Quartz;

namespace Core.Services;

public class RuleDeploymentService(
    IRuleRepository ruleRepository,
    IRuleVersionRepository ruleVersionRepository,
    IRuleDeploymentRepository ruleDeploymentRepository,

    IApprovalRepository approvalRepository,
    IApprovalPolicyRepository approvalPolicyRepository,
    IWebhookConfigurationService webhookConfigurationService,
    IOutboxMessageService outboxMessageService,
    IUnitOfWork unitOfWork,
    ISchedulerFactory schedulerFactory,
    TimeProvider timeProvider,
    IModeContext modeContext) : IRuleDeploymentService
{
    public async Task<RuleDeployment> DeployAsync(DeployRuleRequest deployRuleRequest, CancellationToken stoppingToken)
    {
        var rule = await ruleRepository.GetAsync(deployRuleRequest.RuleId, stoppingToken);
        var targetVersion = await ruleVersionRepository.GetByVersionNumberAsync(rule.RuleId, deployRuleRequest.VersionNumber, stoppingToken);

        var deploymentId = new RuleDeploymentId(Guid.NewGuid());

        var approvalPolicy = await approvalPolicyRepository.FindAsync(rule.OrganizationId, stoppingToken);

        if (ApprovalPolicyResolver.RequiresDeploymentApproval(approvalPolicy))
        {
            return await ProposeAsync(rule, targetVersion, deployRuleRequest, deploymentId, stoppingToken);
        }

        if (deployRuleRequest.Strategy == DeploymentStrategy.AllAtOnce)
        {
            await ruleRepository.DeployVersionAsync(rule.RuleId, targetVersion, stoppingToken);

            var deployment = BuildDeployment(
                rule,
                targetVersion,
                rule.CurrentRuleVersionId,
                DeploymentStrategy.AllAtOnce,
                RuleDeploymentStatus.Completed,
                new RolloutPercentage(100),
                stepPercentage: null,
                stepIntervalSeconds: null,
                completedAt: new CompletedAt(timeProvider.GetUtcNow()),
                deployRuleRequest.DeployedByUserId,
                deploymentId);

            await ruleDeploymentRepository.AddAsync(deployment, stoppingToken);
            await unitOfWork.SaveChangesAsync(stoppingToken);
        }
        else
        {
            var gradualDeployment = BuildDeployment(
                rule,
                targetVersion,
                rule.CurrentRuleVersionId,
                DeploymentStrategy.Gradual,
                RuleDeploymentStatus.InProgress,
                new RolloutPercentage(deployRuleRequest.StepPercentage!.Value),
                deployRuleRequest.StepPercentage,
                deployRuleRequest.StepIntervalSeconds,
                completedAt: null,
                deployRuleRequest.DeployedByUserId,
                deploymentId);

            await ruleDeploymentRepository.AddAsync(gradualDeployment, stoppingToken);
            await unitOfWork.SaveChangesAsync(stoppingToken);

            var activeDeployment = await ruleDeploymentRepository.GetActiveByRuleIdAsync(rule.RuleId, stoppingToken);
            if (activeDeployment is null || activeDeployment.RuleDeploymentId != deploymentId)
            {
                throw new ValidationException(
                [
                    new ValidationFailure(nameof(deployRuleRequest.RuleId), "A deployment is already in progress for this rule")
                    {
                        ErrorCode = HttpStatusCode.Conflict.ToString()
                    }
                ]);
            }

            await ScheduleGradualDeploymentJobAsync(deploymentId, deployRuleRequest.StepIntervalSeconds!.Value, stoppingToken);
        }

        return await ruleDeploymentRepository.GetAsync(deploymentId, stoppingToken);
    }

    public async Task<RuleDeployment> ExecuteApprovedAsync(RuleDeploymentId ruleDeploymentId, CancellationToken stoppingToken)
    {
        var deployment = await ruleDeploymentRepository.GetAsync(ruleDeploymentId, stoppingToken);

        if (deployment.Status != RuleDeploymentStatus.PendingApproval)
        {
            throw new ValidationException(
            [
                new ValidationFailure(nameof(RuleDeployment.RuleDeploymentId), "This deployment is no longer awaiting approval")
                {
                    ErrorCode = HttpStatusCode.Conflict.ToString()
                }
            ]);
        }

        var targetVersion = await ruleVersionRepository.GetAsync(deployment.TargetRuleVersionId, stoppingToken);

        if (deployment.Strategy == DeploymentStrategy.AllAtOnce)
        {
            await ruleRepository.DeployVersionAsync(deployment.RuleId, targetVersion, stoppingToken);

            await ruleDeploymentRepository.UpdateAsync(new UpdateRuleDeploymentRequest
            {
                RuleDeploymentId = ruleDeploymentId,
                Status = RuleDeploymentStatus.Completed,
                CurrentPercentage = new RolloutPercentage(100),
                CompletedAt = new CompletedAt(timeProvider.GetUtcNow())
            }, stoppingToken);

            await unitOfWork.SaveChangesAsync(stoppingToken);

            return await ruleDeploymentRepository.GetAsync(ruleDeploymentId, stoppingToken);
        }

        await ruleDeploymentRepository.UpdateAsync(new UpdateRuleDeploymentRequest
        {
            RuleDeploymentId = ruleDeploymentId,
            Status = RuleDeploymentStatus.InProgress,
            CurrentPercentage = new RolloutPercentage(deployment.StepPercentage!.Value),
            CompletedAt = null
        }, stoppingToken);

        await unitOfWork.SaveChangesAsync(stoppingToken);

        await ScheduleGradualDeploymentJobAsync(ruleDeploymentId, deployment.StepIntervalSeconds!.Value, stoppingToken);

        return await ruleDeploymentRepository.GetAsync(ruleDeploymentId, stoppingToken);
    }

    public async Task CancelDeploymentAsync(RuleDeploymentId ruleDeploymentId, CancellationToken stoppingToken)
    {
        var deployment = await ruleDeploymentRepository.GetAsync(ruleDeploymentId, stoppingToken);

        if (deployment.Status is not (RuleDeploymentStatus.InProgress or RuleDeploymentStatus.PendingApproval))
        {
            return;
        }

        await ruleDeploymentRepository.UpdateAsync(new UpdateRuleDeploymentRequest
        {
            RuleDeploymentId = ruleDeploymentId,
            Status = RuleDeploymentStatus.Cancelled,
            CurrentPercentage = deployment.CurrentPercentage,
            CompletedAt = new CompletedAt(timeProvider.GetUtcNow())
        }, stoppingToken);

        await unitOfWork.SaveChangesAsync(stoppingToken);

        var scheduler = await schedulerFactory.GetScheduler(stoppingToken);
        await scheduler.DeleteJob(new JobKey(ruleDeploymentId.ToString(), QuartzConstants.RuleDeploymentGroup), stoppingToken);
    }

    public Task<List<RuleDeployment>> ListByIdsAsync(IReadOnlyCollection<RuleDeploymentId> ruleDeploymentIds, CancellationToken stoppingToken)
    {
        return ruleDeploymentRepository.ListByIdsAsync(ruleDeploymentIds, stoppingToken);
    }

    public Task<Exists> ActiveDeploymentExistsAsync(RuleId ruleId, CancellationToken stoppingToken)
    {
        return ruleDeploymentRepository.ExistsActiveAsync(ruleId, stoppingToken);
    }

    public Task<Exists> PendingApprovalExistsAsync(RuleId ruleId, CancellationToken stoppingToken)
    {
        return ruleDeploymentRepository.ExistsPendingApprovalAsync(ruleId, stoppingToken);
    }

    public Task<Exists> DeploymentInProgressAsync(OrganizationId organizationId, RuleDeploymentId ruleDeploymentId, CancellationToken stoppingToken)
    {
        return ruleDeploymentRepository.ExistsInProgressAsync(organizationId, ruleDeploymentId, stoppingToken);
    }

    public Task<PagedResponse<RuleDeployment>> ListAsync(ListRuleDeploymentRequest listRuleDeploymentRequest, CancellationToken stoppingToken)
    {
        return ruleDeploymentRepository.ListAsync(listRuleDeploymentRequest, stoppingToken);
    }

    public Task<TotalCount> GetCountAsync(ListRuleDeploymentRequest listRuleDeploymentRequest, CancellationToken stoppingToken)
    {
        return ruleDeploymentRepository.GetCountAsync(listRuleDeploymentRequest, stoppingToken);
    }

    private async Task<RuleDeployment> ProposeAsync(
        Rule rule,
        RuleVersion targetVersion,
        DeployRuleRequest deployRuleRequest,
        RuleDeploymentId ruleDeploymentId,
        CancellationToken stoppingToken)
    {
        var proposal = BuildDeployment(
            rule,
            targetVersion,
            rule.CurrentRuleVersionId,
            deployRuleRequest.Strategy,
            RuleDeploymentStatus.PendingApproval,
            new RolloutPercentage(0),
            deployRuleRequest.StepPercentage,
            deployRuleRequest.StepIntervalSeconds,
            completedAt: null,
            deployRuleRequest.DeployedByUserId,
            ruleDeploymentId);

        await ruleDeploymentRepository.AddAsync(proposal, stoppingToken);

        var createApprovalRequest = new CreateApprovalRequest
        {
            OrganizationId = rule.OrganizationId,
            Subject = ApprovalSubject.RuleDeployment,
            RuleDeploymentId = ruleDeploymentId,
            CaseId = null,
            SuspiciousActivityReportId = null,
            ProposedCaseResolution = null,
            Reason = null,
            RequestedByUserId = deployRuleRequest.DeployedByUserId
        };

        var approval = createApprovalRequest.ToModel(timeProvider, modeContext.CurrentOrLive);

        await approvalRepository.AddAsync(approval, stoppingToken);

        var webhookConfigurations = await webhookConfigurationService.GetAllAsync(rule.OrganizationId, stoppingToken);

        await outboxMessageService.StageManyAsync(
            webhookConfigurations.ToOutboxMessageRequests(WebhookEvents.ApprovalRequested, approval.ToWebhook(), timeProvider.GetUtcNow()),
            stoppingToken);

        await unitOfWork.SaveChangesAsync(stoppingToken);

        return await ruleDeploymentRepository.GetAsync(ruleDeploymentId, stoppingToken);
    }

    private RuleDeployment BuildDeployment(
        Rule rule,
        RuleVersion targetVersion,
        RuleVersionId? previousRuleVersionId,
        DeploymentStrategy strategy,
        RuleDeploymentStatus status,
        RolloutPercentage currentPercentage,
        StepPercentage? stepPercentage,
        StepIntervalSeconds? stepIntervalSeconds,
        CompletedAt? completedAt,
        ApplicationUserId createdByUserId,
        RuleDeploymentId ruleDeploymentId)
    {
        var now = timeProvider.GetUtcNow();

        return new RuleDeployment
        {
            Mode = modeContext.CurrentOrLive,
            RuleDeploymentId = ruleDeploymentId,
            OrganizationId = rule.OrganizationId,
            RuleId = rule.RuleId,
            TargetRuleVersionId = targetVersion.RuleVersionId,
            PreviousRuleVersionId = previousRuleVersionId,
            Strategy = strategy,
            Status = status,
            CurrentPercentage = currentPercentage,
            StepPercentage = stepPercentage,
            StepIntervalSeconds = stepIntervalSeconds,
            CreatedByUserId = createdByUserId,
            CompletedAt = completedAt,
            CreatedAt = new CreatedAt(now),
            UpdatedAt = new UpdatedAt(now)
        };
    }

    private async Task ScheduleGradualDeploymentJobAsync(RuleDeploymentId ruleDeploymentId, int stepIntervalSeconds, CancellationToken stoppingToken)
    {
        var job = JobBuilder.Create<RuleDeploymentJob>()
            .WithIdentity(ruleDeploymentId.ToString(), QuartzConstants.RuleDeploymentGroup)
            .StoreDurably()
            .RequestRecovery()
            .Build();

        var trigger = TriggerBuilder.Create()
            .ForJob(job)
            .WithIdentity(ruleDeploymentId.ToString(), QuartzConstants.RuleDeploymentGroup)
            .StartAt(timeProvider.GetUtcNow().AddSeconds(stepIntervalSeconds))
            .WithSimpleSchedule(schedule => schedule
                .WithIntervalInSeconds(stepIntervalSeconds)
                .RepeatForever())
            .Build();

        var scheduler = await schedulerFactory.GetScheduler(stoppingToken);
        await scheduler.ScheduleJob(job, trigger, stoppingToken);
    }
}
