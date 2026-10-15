using System.Net;
using Core.Configs;
using Core.Constants;
using Core.Documents;
using Core.Enums;
using Core.Helpers;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Mappers;
using Core.Mappers.Notifications;
using Core.Mappers.Webhooks;
using Core.Models.Approvals;
using Core.Models.CaseNotes;
using Core.Models.Cases;
using Core.Models.Common;
using Core.Models.Decisions;
using Core.Models.OutboxMessages;
using Core.Models.Queues;
using Core.Models.Statistics;
using Core.ValueObjects;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Options;
using QuestPDF.Fluent;

namespace Core.Services;

public class CaseService(
    ICaseRepository caseRepository,
    ICaseSlaTargetRepository caseSlaTargetRepository,
    ICaseRuleLinkRepository caseRuleLinkRepository,
    ICaseNoteRepository caseNoteRepository,
    IDecisionRepository decisionRepository,
    IAlertRepository alertRepository,

    IApprovalRepository approvalRepository,
    IApprovalPolicyRepository approvalPolicyRepository,
    IPartyService partyService,
    IQueueRoutingService queueRoutingService,
    IWebhookConfigurationService webhookConfigurationService,
    INotificationSubscriptionService notificationSubscriptionService,
    IOutboxMessageService outboxMessageService,
    IDisplayPreferenceService displayPreferenceService,
    IUnitOfWork unitOfWork,
    IOptions<AppConfig> appConfig,
    TimeProvider timeProvider,
    IModeContext modeContext) : ICaseService
{
    private const string PdfContentType = "application/pdf";

    public async Task<Case> CreateAsync(CreateCaseRequest createCaseRequest, CancellationToken stoppingToken)
    {
        var slaTargets = await caseSlaTargetRepository.GetAsync(createCaseRequest.OrganizationId, stoppingToken);

        var @case = createCaseRequest.ToModel(slaTargets, appConfig.Value.CaseConfig, timeProvider, modeContext.CurrentOrLive);
        var friendlyName = await caseRepository.GenerateFriendlyName(stoppingToken);

        var queueRouting = await queueRoutingService.RouteAsync(ToRouteCaseRequest(createCaseRequest), stoppingToken);

        @case = @case with
        {
            FriendlyName = friendlyName,
            QueueId = queueRouting.QueueId,
            AssignedToUserId = queueRouting.AssignedToUserId,
            AssignedAt = queueRouting.AssignedToUserId.HasValue ? new AssignedAt(timeProvider.GetUtcNow()) : null,
            Status = queueRouting.AssignedToUserId.HasValue ? CaseStatus.Assigned : CaseStatus.Pending
        };

        await caseRepository.AddAsync(@case, stoppingToken);

        var caseRuleLinks = createCaseRequest.RuleEvaluationIds.Select(x => new CaseRuleEvaluationLink
        {
            CaseId = @case.CaseId,
            RuleEvaluationId = x,
            CreatedAt = new CreatedAt(timeProvider.GetUtcNow()),
            UpdatedAt = new UpdatedAt(timeProvider.GetUtcNow())
        }).ToList();

        await caseRuleLinkRepository.AddRangeAsync(caseRuleLinks, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);

        @case = await GetAsync(@case.CaseId, stoppingToken);
        await SendCaseWebhook(WebhookEvents.CaseCreated, @case, stoppingToken);

        if (@case.AssignedToUserId.HasValue)
        {
            await SendCaseWebhook(WebhookEvents.CaseAssigned, @case, stoppingToken);
        }

        await SendSeverityNotifications(@case, stoppingToken);

        return @case;
    }

    public Task<Case> GetAsync(CaseId caseId, CancellationToken stoppingToken)
    {
        return caseRepository.GetAsync(caseId, stoppingToken);
    }

    public async Task<EvidencePack> GetEvidencePackAsync(CaseId caseId, CancellationToken stoppingToken)
    {
        var evidencePackSource = await caseRepository.GetEvidencePackSourceAsync(caseId, stoppingToken);

        var document = new EvidencePackDocument(evidencePackSource, timeProvider.GetUtcNow());

        return new EvidencePack
        {
            FileName = new FileName($"evidence-pack-{evidencePackSource.Case.FriendlyName}.pdf"),
            ContentType = new ContentType(PdfContentType),
            Content = document.GeneratePdf()
        };
    }

    public Task<Exists> ExistsAsync(OrganizationId organizationId, CaseId caseId, IncludeDeleted includeDeleted, CancellationToken stoppingToken)
    {
        return caseRepository.ExistsAsync(organizationId, caseId, includeDeleted, stoppingToken);
    }

    public Task<PagedResponse<Case>> ListAsync(ListCaseRequest listCaseRequest, CancellationToken stoppingToken)
    {
        return caseRepository.ListAsync(listCaseRequest, stoppingToken);
    }

    public async Task AssignAsync(CaseId caseId, ApplicationUserId userId, CancellationToken stoppingToken)
    {
        await caseRepository.AssignAsync(caseId, userId, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);

        var @case = await GetAsync(caseId, stoppingToken);
        await SendCaseWebhook(WebhookEvents.CaseAssigned, @case, stoppingToken);
    }

    public async Task UnassignAsync(CaseId caseId, CancellationToken stoppingToken)
    {
        await caseRepository.UnassignAsync(caseId, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);

        var @case = await GetAsync(caseId, stoppingToken);
        await SendCaseWebhook(WebhookEvents.CaseUnassigned, @case, stoppingToken);
    }

    public async Task<Approval?> ResolveAsync(ResolveCaseRequest resolveCaseRequest, CancellationToken stoppingToken)
    {
        var approvalPolicy = await approvalPolicyRepository.FindAsync(resolveCaseRequest.OrganizationId, stoppingToken);

        if (approvalPolicy is null)
        {
            await CloseAsync(resolveCaseRequest, stoppingToken);

            return null;
        }

        var pending = await approvalRepository.FindPendingByCaseAsync(resolveCaseRequest.CaseId, stoppingToken);

        if (pending is not null)
        {
            throw new ValidationException(
            [
                new ValidationFailure(nameof(ResolveCaseRequest.CaseId), "This case is already awaiting approval")
                {
                    ErrorCode = HttpStatusCode.Conflict.ToString()
                }
            ]);
        }

        var @case = await caseRepository.GetAsync(resolveCaseRequest.CaseId, stoppingToken);

        if (!await RequiresApprovalAsync(approvalPolicy, @case, stoppingToken))
        {
            await CloseAsync(resolveCaseRequest, stoppingToken);

            return null;
        }

        return await RaiseApprovalAsync(@case, resolveCaseRequest, stoppingToken);
    }

    public Task ApplyApprovedResolutionAsync(ResolveCaseRequest resolveCaseRequest, CancellationToken stoppingToken)
    {
        return CloseAsync(resolveCaseRequest, stoppingToken);
    }

    private async Task CloseAsync(ResolveCaseRequest resolveCaseRequest, CancellationToken stoppingToken)
    {
        var caseClosure = new CaseClosure
        {
            Status = resolveCaseRequest.Status,
            Disposition = resolveCaseRequest.Disposition,
            CaseReasonCodeId = resolveCaseRequest.CaseReasonCodeId
        };

        var @case = await caseRepository.ResolveAsync(resolveCaseRequest.CaseId, caseClosure, stoppingToken);

        var resolveDecisionRequest = new ResolveDecisionRequest
        {
            CaseId = resolveCaseRequest.CaseId,
            Verdict = DecisionVerdictResolver.ForCaseStatus(resolveCaseRequest.Status),
            ResolvedByUserId = resolveCaseRequest.ResolvedByUserId
        };

        var decisions = await decisionRepository.ResolveByCaseAsync(resolveDecisionRequest, stoppingToken);

        await alertRepository.CloseByCaseAsync(resolveCaseRequest.CaseId, stoppingToken);

        await StageWebhook(WebhookEvents.CaseResolved, @case.OrganizationId, @case.ToWebhook(), stoppingToken);

        foreach (var decision in decisions)
        {
            await StageWebhook(WebhookEvents.DecisionResolved, decision.OrganizationId, decision.ToWebhook(), stoppingToken);
        }

        await AddNoteAsync(@case, resolveCaseRequest.Note, resolveCaseRequest.ResolvedByUserId, stoppingToken);

        await unitOfWork.SaveChangesAsync(stoppingToken);
    }

    public async Task ChangeStatusAsync(ChangeCaseStatusRequest changeCaseStatusRequest, CancellationToken stoppingToken)
    {
        var @case = await caseRepository.ChangeStatusAsync(changeCaseStatusRequest.CaseId, changeCaseStatusRequest.Status, stoppingToken);

        await StageWebhook(WebhookEvents.CaseStatusChanged, @case.OrganizationId, @case.ToWebhook(), stoppingToken);

        await AddNoteAsync(@case, changeCaseStatusRequest.Note, changeCaseStatusRequest.ChangedByUserId, stoppingToken);

        await unitOfWork.SaveChangesAsync(stoppingToken);
    }

    public async Task ReopenAsync(ReopenCaseRequest reopenCaseRequest, CancellationToken stoppingToken)
    {
        var @case = await caseRepository.ReopenAsync(reopenCaseRequest.CaseId, stoppingToken);

        await alertRepository.ReopenByCaseAsync(reopenCaseRequest.CaseId, stoppingToken);

        await StageWebhook(WebhookEvents.CaseReopened, @case.OrganizationId, @case.ToWebhook(), stoppingToken);

        await AddNoteAsync(@case, reopenCaseRequest.Reason, reopenCaseRequest.ReopenedByUserId, stoppingToken);

        await unitOfWork.SaveChangesAsync(stoppingToken);
    }

    public async Task DeleteAsync(CaseId caseId, CancellationToken stoppingToken)
    {
        await caseRepository.DeleteAsync(caseId, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);
    }

    public async Task RecoverAsync(CaseId caseId, CancellationToken stoppingToken)
    {
        await caseRepository.RecoverAsync(caseId, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);
    }

    public Task<TotalCount> GetCountAsync(ListCaseRequest listCaseRequest, CancellationToken stoppingToken)
    {
        return caseRepository.GetCountAsync(listCaseRequest, stoppingToken);
    }

    public Task<Dictionary<CaseStatus, TotalCount>> CountByStatusAsync(ListCaseRequest listCaseRequest, CancellationToken stoppingToken)
    {
        return caseRepository.CountByStatusAsync(listCaseRequest, stoppingToken);
    }

    public Task<TimeSpan?> GetAverageResolutionTimeAsync(StatisticRequest statisticRequest, CancellationToken stoppingToken)
    {
        return caseRepository.GetAverageResolutionTimeAsync(statisticRequest, stoppingToken);
    }

    public Task<TotalCount> CountBreachedAsync(ListCaseRequest listCaseRequest, CancellationToken stoppingToken)
    {
        return caseRepository.CountBreachedAsync(listCaseRequest, stoppingToken);
    }

    public async Task<IReadOnlyDictionary<RuleSeverity, SlaTarget>> GetSlaTargetsAsync(OrganizationId organizationId, CancellationToken stoppingToken)
    {
        var declared = await caseSlaTargetRepository.GetAsync(organizationId, stoppingToken);

        return Enum.GetValues<RuleSeverity>()
            .ToDictionary(x => x, x => CaseDueResolver.ResolveTarget(x, declared, appConfig.Value.CaseConfig));
    }

    public async Task SetSlaTargetsAsync(SetCaseSlaTargetsRequest setCaseSlaTargetsRequest, CancellationToken stoppingToken)
    {
        await caseSlaTargetRepository.SetAsync(setCaseSlaTargetsRequest, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);
    }

    private async Task<bool> RequiresApprovalAsync(ApprovalPolicy approvalPolicy, Case @case, CancellationToken stoppingToken)
    {
        if (ApprovalPolicyResolver.MatchesMinimumSeverity(approvalPolicy, @case.Severity))
        {
            return true;
        }

        if (!ApprovalPolicyResolver.DeclaresPayloadCondition(approvalPolicy))
        {
            return false;
        }

        var payload = await caseRepository.GetPayloadAsync(@case.CaseId, stoppingToken);
        var parties = await partyService.PeekAsync(@case.OrganizationId, payload, stoppingToken);

        return ApprovalPolicyResolver.RequiresCaseApproval(
            approvalPolicy, @case.Severity, payload, parties, timeProvider.GetUtcNow());
    }

    private async Task<Approval> RaiseApprovalAsync(Case @case, ResolveCaseRequest resolveCaseRequest, CancellationToken stoppingToken)
    {
        var createApprovalRequest = new CreateApprovalRequest
        {
            OrganizationId = @case.OrganizationId,
            Subject = ApprovalSubject.CaseResolution,
            RuleDeploymentId = null,
            CaseId = @case.CaseId,
            SuspiciousActivityReportId = null,
            ProposedCaseResolution = new ProposedCaseResolution
            {
                Status = resolveCaseRequest.Status,
                Disposition = resolveCaseRequest.Disposition,
                CaseReasonCodeId = resolveCaseRequest.CaseReasonCodeId,

                Note = resolveCaseRequest.Note
            },
            Reason = null,
            RequestedByUserId = resolveCaseRequest.ResolvedByUserId
        };

        var approval = createApprovalRequest.ToModel(timeProvider, modeContext.CurrentOrLive);

        await approvalRepository.AddAsync(approval, stoppingToken);

        await StageWebhook(WebhookEvents.ApprovalRequested, @case.OrganizationId, approval.ToWebhook(), stoppingToken);

        await unitOfWork.SaveChangesAsync(stoppingToken);

        return approval;
    }

    private static RouteCaseRequest ToRouteCaseRequest(CreateCaseRequest createCaseRequest)
    {
        return new RouteCaseRequest
        {
            OrganizationId = createCaseRequest.OrganizationId,
            Severity = createCaseRequest.Severity,
            RuleIds = createCaseRequest.RuleIds,
            Payload = createCaseRequest.Payload,
            Parties = createCaseRequest.Parties
        };
    }

    private async Task AddNoteAsync(Case @case, Message? note, ApplicationUserId createdByUserId, CancellationToken stoppingToken)
    {
        if (!note.HasValue)
        {
            return;
        }

        var createCaseNoteRequest = new CreateCaseNoteRequest
        {
            CaseId = @case.CaseId,
            OrganizationId = @case.OrganizationId,
            Message = note.Value,
            CreatedByUserId = createdByUserId
        };

        await caseNoteRepository.AddAsync(createCaseNoteRequest.ToModel(timeProvider, modeContext.CurrentOrLive), stoppingToken);
    }

    private async Task SendCaseWebhook(string @event, Case @case, CancellationToken stoppingToken)
    {
        var createOutboxMessageRequests = await BuildOutboxMessageRequests(@event, @case.OrganizationId, @case.ToWebhook(), stoppingToken);

        await outboxMessageService.CreateManyAsync(createOutboxMessageRequests, stoppingToken);
    }

    private async Task StageWebhook<T>(string @event, OrganizationId organizationId, T payload, CancellationToken stoppingToken)
    {
        var createOutboxMessageRequests = await BuildOutboxMessageRequests(@event, organizationId, payload, stoppingToken);

        await outboxMessageService.StageManyAsync(createOutboxMessageRequests, stoppingToken);
    }

    private async Task<List<CreateOutboxMessageRequest>> BuildOutboxMessageRequests<T>(string @event, OrganizationId organizationId, T payload, CancellationToken stoppingToken)
    {
        var webhookConfigurations = await webhookConfigurationService.GetAllAsync(organizationId, stoppingToken);

        return webhookConfigurations.ToOutboxMessageRequests(@event, payload, timeProvider.GetUtcNow());
    }

    private async Task SendSeverityNotifications(Case @case, CancellationToken stoppingToken)
    {
        var notificationSubscriptions = await notificationSubscriptionService.ListEnabledAsync(@case.OrganizationId, stoppingToken);

        var matched = notificationSubscriptions.ForSeverity(@case.Severity);

        if (matched.Count == 0)
        {
            return;
        }

        var preferences = await displayPreferenceService.ForOrganizationAsync(@case.OrganizationId, stoppingToken);

        var outboxMessageRequests = matched.ToOutboxMessageRequests(
            preferences,
            translator => NotificationMessageBuilder.ForHighSeverityCase(@case, translator, timeProvider.GetUtcNow()));

        await outboxMessageService.CreateManyAsync(outboxMessageRequests, stoppingToken);
    }
}