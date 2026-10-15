using Core.Configs;
using Core.Constants;
using Core.Enums;
using Core.Exceptions;
using Core.Helpers;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Mappers;
using Core.Mappers.Webhooks;
using Core.Models.CaseNotes;
using Core.Models.Cases;
using Core.Models.Decisions;
using Core.Models.WebhookConfigurations;
using Core.ValueObjects;
using Microsoft.Extensions.Options;

namespace Core.Services;

public class DecisionService(
    IDecisionRepository decisionRepository,
    IDecisionRuleLinkRepository decisionRuleLinkRepository,
    ICaseRepository caseRepository,
    ICaseNoteRepository caseNoteRepository,
    IAlertRepository alertRepository,
    IWebhookConfigurationService webhookConfigurationService,
    IOutboxMessageService outboxMessageService,
    IUnitOfWork unitOfWork,
    IOptions<AppConfig> appConfig,
    TimeProvider timeProvider,
    IModeContext modeContext) : IDecisionService
{
    public async Task<Decision> RecordAsync(RecordDecisionRequest recordDecisionRequest, CancellationToken stoppingToken)
    {
        var existing = await decisionRepository.FindByTransactionAsync(recordDecisionRequest.TransactionId, stoppingToken);
        if (existing is not null)
        {
            // A concurrent evaluation may have recorded its decision after the caller checked; the caller must roll back and adopt the winner.
            if (recordDecisionRequest.CaseId.HasValue && existing.CaseId != recordDecisionRequest.CaseId)
            {
                throw new DecisionAlreadyRecordedException(recordDecisionRequest.TransactionId);
            }

            return existing;
        }

        var decision = recordDecisionRequest.ToModel(timeProvider, modeContext.CurrentOrLive);

        await decisionRepository.AddAsync(decision, stoppingToken);

        var now = timeProvider.GetUtcNow();
        var decisionRuleLinks = recordDecisionRequest.RuleEvaluationIds.ConvertAll(x => new DecisionRuleEvaluationLink
        {
            DecisionId = decision.DecisionId,
            RuleEvaluationId = x,
            CreatedAt = new CreatedAt(now),
            UpdatedAt = new UpdatedAt(now)
        });

        await decisionRuleLinkRepository.AddRangeAsync(decisionRuleLinks, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);

        return await decisionRepository.GetAsync(decision.DecisionId, stoppingToken);
    }

    public Task<Decision?> FindByTransactionAsync(TransactionId transactionId, CancellationToken stoppingToken)
    {
        return decisionRepository.FindByTransactionAsync(transactionId, stoppingToken);
    }

    public async Task<Count> ExpireDueAsync(CancellationToken stoppingToken)
    {
        var dueDecisions = await decisionRepository.ListDueForExpiryAsync(appConfig.Value.DecisionConfig.HoldExpiryBatchSize, stoppingToken);

        if (dueDecisions.Count == 0)
        {
            return new Count(0);
        }

        var verdicts = dueDecisions.ToDictionary(
            x => x.DecisionId,
            x => DecisionVerdictResolver.ForHoldExpiry(x.HoldExpiryVerdict!.Value));

        var decisions = await decisionRepository.ExpireManyAsync(verdicts, stoppingToken);

        var caseIds = decisions.Where(x => x.CaseId.HasValue).Select(x => x.CaseId!.Value).ToList();

        var settledCaseIds = await ListSettledCasesAsync(caseIds, decisions.ConvertAll(x => x.DecisionId), stoppingToken);

        var cases = await caseRepository.ExpireManyAsync(settledCaseIds, stoppingToken);

        var webhookConfigurationsByOrganization = new Dictionary<OrganizationId, List<WebhookConfiguration>>();

        foreach (var @case in cases)
        {
            await alertRepository.CloseByCaseAsync(@case.CaseId, stoppingToken);

            await StageWebhookAsync(WebhookEvents.CaseResolved, @case.OrganizationId, @case.ToWebhook(), webhookConfigurationsByOrganization, stoppingToken);
        }

        foreach (var decision in decisions)
        {
            await StageWebhookAsync(WebhookEvents.DecisionResolved, decision.OrganizationId, decision.ToWebhook(), webhookConfigurationsByOrganization, stoppingToken);
        }

        await unitOfWork.SaveChangesAsync(stoppingToken);

        return new Count(decisions.Count);
    }

    public async Task ResolveAsync(ResolveDecisionByIdRequest resolveDecisionByIdRequest, CancellationToken stoppingToken)
    {
        var verdict = DecisionVerdictResolver.ForCaseStatus(resolveDecisionByIdRequest.Verdict);

        var decision = await decisionRepository.ResolveAsync(
            resolveDecisionByIdRequest.DecisionId,
            verdict,
            resolveDecisionByIdRequest.ResolvedByUserId,
            stoppingToken);

        if (decision is null)
        {
            return;
        }

        var webhookConfigurations = await webhookConfigurationService.GetAllAsync(decision.OrganizationId, stoppingToken);

        await outboxMessageService.StageManyAsync(
            webhookConfigurations.ToOutboxMessageRequests(WebhookEvents.DecisionResolved, decision.ToWebhook(), timeProvider.GetUtcNow()),
            stoppingToken);

        await CloseSettledCaseAsync(resolveDecisionByIdRequest, decision, verdict, webhookConfigurations, stoppingToken);

        await unitOfWork.SaveChangesAsync(stoppingToken);
    }

    private async Task CloseSettledCaseAsync(
        ResolveDecisionByIdRequest resolveDecisionByIdRequest,
        Decision decision,
        DecisionVerdict verdict,
        List<WebhookConfiguration> webhookConfigurations,
        CancellationToken stoppingToken)
    {
        if (!decision.CaseId.HasValue)
        {
            return;
        }

        var caseId = decision.CaseId.Value;

        var exists = await caseRepository.ExistsAsync(decision.OrganizationId, caseId, new IncludeDeleted(false), stoppingToken);
        if (!exists)
        {
            return;
        }

        var statusesByCase = await decisionRepository.ListStatusesByCaseAsync([caseId], [decision.DecisionId], stoppingToken);
        var otherStatuses = statusesByCase[caseId];

        if (otherStatuses.Exists(x => !x.IsTerminal()))
        {
            return;
        }

        var status = verdict.Status == DecisionStatus.Blocked || otherStatuses.Contains(DecisionStatus.Blocked)
            ? CaseStatus.Rejected
            : CaseStatus.Approved;

        var caseClosure = new CaseClosure
        {
            Status = status,
            Disposition = resolveDecisionByIdRequest.Disposition,
            CaseReasonCodeId = resolveDecisionByIdRequest.CaseReasonCodeId
        };

        var @case = await caseRepository.ResolveAsync(caseId, caseClosure, stoppingToken);

        await alertRepository.CloseByCaseAsync(caseId, stoppingToken);

        await outboxMessageService.StageManyAsync(
            webhookConfigurations.ToOutboxMessageRequests(WebhookEvents.CaseResolved, @case.ToWebhook(), timeProvider.GetUtcNow()),
            stoppingToken);

        // Last: a note's CreatedByUser leaves a stub user in the change tracker that later reads would resolve against.
        if (resolveDecisionByIdRequest.Note.HasValue)
        {
            var createCaseNoteRequest = new CreateCaseNoteRequest
            {
                CaseId = @case.CaseId,
                OrganizationId = @case.OrganizationId,
                Message = resolveDecisionByIdRequest.Note.Value,
                CreatedByUserId = resolveDecisionByIdRequest.ResolvedByUserId
            };

            await caseNoteRepository.AddAsync(createCaseNoteRequest.ToModel(timeProvider, modeContext.CurrentOrLive), stoppingToken);
        }
    }

    private async Task<List<CaseId>> ListSettledCasesAsync(
        IReadOnlyCollection<CaseId> caseIds,
        IReadOnlyCollection<DecisionId> settlingDecisionIds,
        CancellationToken stoppingToken)
    {
        var statusesByCase = await decisionRepository.ListStatusesByCaseAsync(caseIds, settlingDecisionIds, stoppingToken);

        return statusesByCase
            .Where(x => x.Value.TrueForAll(y => y.IsTerminal()))
            .Select(x => x.Key)
            .ToList();
    }

    public async Task<Exists> SettlesCaseAsync(DecisionId decisionId, CancellationToken stoppingToken)
    {
        var decision = await decisionRepository.FindAsync(decisionId, stoppingToken);

        if (decision?.CaseId is null)
        {
            return new Exists(false);
        }

        if (decision.Status.IsTerminal())
        {
            return new Exists(false);
        }

        var caseId = decision.CaseId.Value;

        var exists = await caseRepository.ExistsAsync(decision.OrganizationId, caseId, new IncludeDeleted(false), stoppingToken);

        if (!exists)
        {
            return new Exists(false);
        }

        var statusesByCase = await decisionRepository.ListStatusesByCaseAsync([caseId], [decisionId], stoppingToken);

        return new Exists(statusesByCase[caseId].TrueForAll(x => x.IsTerminal()));
    }

    public Task<Exists> ExistsAsync(OrganizationId organizationId, DecisionId decisionId, CancellationToken stoppingToken)
    {
        return decisionRepository.ExistsAsync(organizationId, decisionId, stoppingToken);
    }

    private async Task StageWebhookAsync<T>(
        string @event,
        OrganizationId organizationId,
        T payload,
        Dictionary<OrganizationId, List<WebhookConfiguration>> webhookConfigurationsByOrganization,
        CancellationToken stoppingToken)
    {
        if (!webhookConfigurationsByOrganization.TryGetValue(organizationId, out var webhookConfigurations))
        {
            webhookConfigurations = await webhookConfigurationService.GetAllAsync(organizationId, stoppingToken);
            webhookConfigurationsByOrganization[organizationId] = webhookConfigurations;
        }

        await outboxMessageService.StageManyAsync(
            webhookConfigurations.ToOutboxMessageRequests(@event, payload, timeProvider.GetUtcNow()),
            stoppingToken);
    }
}
