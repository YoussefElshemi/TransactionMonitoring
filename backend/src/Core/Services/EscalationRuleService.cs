using Core.Configs;
using Core.Constants;
using Core.Helpers;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Mappers;
using Core.Mappers.Webhooks;
using Core.Models.Common;
using Core.Models.Escalations;
using Core.Models.Parties;
using Core.ValueObjects;
using Microsoft.Extensions.Options;

namespace Core.Services;

public class EscalationRuleService(
    IEscalationRuleRepository escalationRuleRepository,
    ICaseRepository caseRepository,
    IPartyService partyService,
    IWebhookConfigurationService webhookConfigurationService,
    IOutboxMessageService outboxMessageService,
    IUnitOfWork unitOfWork,
    IOptions<AppConfig> appConfig,
    TimeProvider timeProvider,
    IModeContext modeContext) : IEscalationRuleService
{
    public async Task<EscalationRule> CreateAsync(CreateEscalationRuleRequest createEscalationRuleRequest, CancellationToken stoppingToken)
    {
        var escalationRule = createEscalationRuleRequest.ToModel(timeProvider, modeContext.CurrentOrLive);

        await escalationRuleRepository.AddAsync(escalationRule, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);

        return await GetAsync(escalationRule.EscalationRuleId, new IncludeDeleted(false), stoppingToken);
    }

    public Task<EscalationRule> GetAsync(EscalationRuleId escalationRuleId, IncludeDeleted includeDeleted, CancellationToken stoppingToken)
    {
        return escalationRuleRepository.GetAsync(escalationRuleId, includeDeleted, stoppingToken);
    }

    public Task<EscalationRule?> FindByNameAsync(OrganizationId organizationId, EscalationRuleName name, CancellationToken stoppingToken)
    {
        return escalationRuleRepository.FindByNameAsync(organizationId, name, stoppingToken);
    }

    public Task<Exists> ExistsAsync(OrganizationId organizationId, EscalationRuleId escalationRuleId, IncludeDeleted includeDeleted, CancellationToken stoppingToken)
    {
        return escalationRuleRepository.ExistsAsync(organizationId, escalationRuleId, includeDeleted, stoppingToken);
    }

    public Task<PagedResponse<EscalationRule>> ListAsync(ListEscalationRuleRequest listEscalationRuleRequest, CancellationToken stoppingToken)
    {
        return escalationRuleRepository.ListAsync(listEscalationRuleRequest, stoppingToken);
    }

    public Task<TotalCount> GetCountAsync(ListEscalationRuleRequest listEscalationRuleRequest, CancellationToken stoppingToken)
    {
        return escalationRuleRepository.GetCountAsync(listEscalationRuleRequest, stoppingToken);
    }

    public async Task UpdateAsync(UpdateEscalationRuleRequest updateEscalationRuleRequest, CancellationToken stoppingToken)
    {
        await escalationRuleRepository.UpdateAsync(updateEscalationRuleRequest, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);
    }

    public async Task DeleteAsync(EscalationRuleId escalationRuleId, CancellationToken stoppingToken)
    {
        await escalationRuleRepository.DeleteAsync(escalationRuleId, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);
    }

    public async Task RecoverAsync(EscalationRuleId escalationRuleId, CancellationToken stoppingToken)
    {
        await escalationRuleRepository.RecoverAsync(escalationRuleId, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);
    }

    public async Task<Count> EscalateDueAsync(CancellationToken stoppingToken)
    {
        var organizationIds = await escalationRuleRepository.ListOrganizationsWithActiveRulesAsync(stoppingToken);

        var escalated = 0;

        foreach (var organizationId in organizationIds)
        {
            escalated += await EscalateOrganizationAsync(organizationId, stoppingToken);
        }

        return new Count(escalated);
    }

    private async Task<int> EscalateOrganizationAsync(OrganizationId organizationId, CancellationToken stoppingToken)
    {
        var escalationRules = await escalationRuleRepository.ListActiveAsync(organizationId, stoppingToken);

        if (escalationRules.Count == 0)
        {
            return 0;
        }

        var batchSize = new Count(appConfig.Value.CaseConfig.EscalationBatchSize);
        var caseEscalations = new List<CaseEscalation>();
        var now = timeProvider.GetUtcNow();

        CreatedAt? afterCreatedAt = null;
        CaseId? afterCaseId = null;

        while (true)
        {
            var candidates = await caseRepository.ListEscalationCandidatesAsync(new ListEscalationCandidateRequest
            {
                OrganizationId = organizationId,
                BatchSize = batchSize,
                AfterCreatedAt = afterCreatedAt,
                AfterCaseId = afterCaseId
            }, stoppingToken);

            if (candidates.Count == 0)
            {
                break;
            }

            caseEscalations.AddRange(await MatchAsync(organizationId, escalationRules, candidates, now, stoppingToken));

            if (candidates.Count < batchSize)
            {
                break;
            }

            var last = candidates[^1];
            afterCreatedAt = last.Case.CreatedAt;
            afterCaseId = last.Case.CaseId;
        }

        if (caseEscalations.Count == 0)
        {
            return 0;
        }

        var cases = await caseRepository.EscalateManyAsync(caseEscalations, stoppingToken);

        var webhookConfigurations = await webhookConfigurationService.GetAllAsync(organizationId, stoppingToken);

        foreach (var @case in cases)
        {
            await outboxMessageService.StageManyAsync(
                webhookConfigurations.ToOutboxMessageRequests(WebhookEvents.CaseEscalated, @case.ToWebhook(), now),
                stoppingToken);
        }

        await unitOfWork.SaveChangesAsync(stoppingToken);

        return cases.Count;
    }

    private async Task<List<CaseEscalation>> MatchAsync(
        OrganizationId organizationId,
        IReadOnlyCollection<EscalationRule> escalationRules,
        List<EscalationCandidate> candidates,
        DateTimeOffset now,
        CancellationToken stoppingToken)
    {
        var partiesPerCandidate = await PeekPartiesAsync(organizationId, escalationRules, candidates, stoppingToken);

        var caseEscalations = new List<CaseEscalation>();

        for (var index = 0; index < candidates.Count; index++)
        {
            var candidate = candidates[index];
            var escalationRule = EscalationMatcher.FirstMatch(escalationRules, candidate, partiesPerCandidate[index], now);

            if (escalationRule is null)
            {
                continue;
            }

            caseEscalations.Add(ToCaseEscalation(escalationRule, candidate, now));
        }

        return caseEscalations;
    }

    private async Task<List<List<PartyFacts>>> PeekPartiesAsync(
        OrganizationId organizationId,
        IReadOnlyCollection<EscalationRule> escalationRules,
        List<EscalationCandidate> candidates,
        CancellationToken stoppingToken)
    {
        if (!escalationRules.Any(x => x.Matcher is not null))
        {
            return candidates.ConvertAll(_ => new List<PartyFacts>());
        }

        return await partyService.PeekManyAsync(organizationId, candidates.ConvertAll(x => x.Payload), stoppingToken);
    }

    private static CaseEscalation ToCaseEscalation(EscalationRule escalationRule, EscalationCandidate escalationCandidate, DateTimeOffset now)
    {
        var raiseSeverityTo = escalationRule.RaiseSeverityTo.HasValue &&
                              escalationRule.RaiseSeverityTo.Value > escalationCandidate.Case.Severity
            ? escalationRule.RaiseSeverityTo
            : null;

        var targetQueueId = escalationRule.TargetQueueId.HasValue &&
                            escalationRule.TargetQueueId != escalationCandidate.Case.QueueId
            ? escalationRule.TargetQueueId
            : null;

        return new CaseEscalation
        {
            CaseId = escalationCandidate.Case.CaseId,
            EscalationRuleId = escalationRule.EscalationRuleId,
            EscalatedAt = new EscalatedAt(now),
            TargetQueueId = targetQueueId,
            RaiseSeverityTo = raiseSeverityTo
        };
    }
}
