using System.Text.Json.Nodes;
using Core.Configs;
using Core.Enums;
using Core.Exceptions;
using Core.Helpers;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Mappers;
using Core.Models.Alerts;
using Core.Models.Cases;
using Core.Models.Common;
using Core.Models.Decisions;
using Core.Models.Parties;
using Core.Models.RuleEvaluations;
using Core.Models.Rules;
using Core.Models.RuleSets;
using Core.Models.Transactions;
using Core.Models.Usages;
using Core.Models.VelocityObservations;
using Core.ValueObjects;
using Microsoft.Extensions.Options;

namespace Core.Services;

public class RuleSetService(
    IRuleSetRepository ruleSetRepository,
    IUsageService usageService,
    ICaseService caseService,
    IAlertService alertService,
    IDecisionService decisionService,
    IRuleEvaluationService ruleEvaluationService,
    ITransactionService transactionService,
    ITransactionSchemaService transactionSchemaService,
    IPayloadProtectionService payloadProtectionService,
    IPartyService partyService,
    IVelocityService velocityService,
    IUnitOfWork unitOfWork,
    IOptions<AppConfig> appConfig,
    TimeProvider timeProvider,
    ISandboxCreditService sandboxCreditService,
    IModeContext modeContext) : IRuleSetService
{
    public async Task<RuleSet> CreateAsync(CreateRuleSetRequest createRuleSetRequest, CancellationToken stoppingToken)
    {
        var ruleSet = createRuleSetRequest.ToModel(timeProvider, modeContext.CurrentOrLive);

        await ruleSetRepository.AddAsync(ruleSet, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);

        return await GetAsync(ruleSet.RuleSetId, stoppingToken);
    }

    public async Task UpdateAsync(UpdateRuleSetRequest updateRuleSetRequest, CancellationToken stoppingToken)
    {
        await ruleSetRepository.UpdateAsync(updateRuleSetRequest, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);
    }

    public Task<Exists> ExistsAsync(OrganizationId organizationId, RuleSetId ruleSetId, IncludeDeleted includeDeleted, CancellationToken stoppingToken)
    {
        return ruleSetRepository.ExistsAsync(organizationId, ruleSetId, includeDeleted, stoppingToken);
    }

    public Task<PagedResponse<RuleSet>> ListAsync(ListRuleSetRequest listRuleSetRequest, CancellationToken stoppingToken)
    {
        return ruleSetRepository.ListAsync(listRuleSetRequest, stoppingToken);
    }

    public Task<RuleSet> GetAsync(RuleSetId ruleSetId, CancellationToken stoppingToken)
    {
        return ruleSetRepository.GetAsync(ruleSetId, stoppingToken);
    }

    public Task<List<RuleSet>> ListByIdsAsync(IReadOnlyCollection<RuleSetId> ruleSetIds, CancellationToken stoppingToken)
    {
        return ruleSetRepository.ListByIdsAsync(ruleSetIds, stoppingToken);
    }

    public async Task DeleteAsync(RuleSetId ruleSetId, CancellationToken stoppingToken)
    {
        await ruleSetRepository.DeleteAsync(ruleSetId, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);
    }

    public async Task RecoverAsync(RuleSetId ruleSetId, CancellationToken stoppingToken)
    {
        await ruleSetRepository.RecoverAsync(ruleSetId, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);
    }

    public Task<TotalCount> GetCountAsync(ListRuleSetRequest listRuleSetRequest, CancellationToken stoppingToken)
    {
        return ruleSetRepository.GetCountAsync(listRuleSetRequest, stoppingToken);
    }

    public async Task<EvaluateRuleSetResponse> EvaluateAsync(EvaluateRuleSetRequest evaluateRuleSetRequest, CancellationToken stoppingToken)
    {
        SandboxTriggerResolver.ThrowIfError(modeContext.CurrentOrLive, evaluateRuleSetRequest.Payload, evaluateRuleSetRequest.ExternalReference);

        var ruleSet = await ruleSetRepository.GetAsync(evaluateRuleSetRequest.RuleSetId, stoppingToken);

        await transactionSchemaService.EnforcePayloadAsync(ruleSet.OrganizationId, evaluateRuleSetRequest.Payload, stoppingToken);

        evaluateRuleSetRequest = evaluateRuleSetRequest with
        {
            Payload = await TokeniseAsync(ruleSet.OrganizationId, evaluateRuleSetRequest.Payload, stoppingToken)
        };

        var rulesToEvaluate = ruleSet.Rules.Where(x => !x.IsDeleted && x.IsEnabled).ToList();

        await sandboxCreditService.EnforceAsync(
            ruleSet.OrganizationId, BillableResource.RuleSetEvaluation, new Resource(evaluateRuleSetRequest.RuleSetId.ToString()), new Count(rulesToEvaluate.Count), stoppingToken);

        var createTransactionRequest = new CreateTransactionRequest
        {
            OrganizationId = ruleSet.OrganizationId,
            ExternalReference = evaluateRuleSetRequest.ExternalReference,
            Payload = evaluateRuleSetRequest.Payload
        };

        var transaction = await transactionService.GetOrCreateAsync(createTransactionRequest, stoppingToken);

        var parties = await RecordPartiesAsync(ruleSet.OrganizationId, transaction, stoppingToken);

        var threshold = ruleSet.ReviewThreshold ?? new ReviewThreshold(appConfig.Value.RuleConfig.DefaultReviewThreshold);

        var now = timeProvider.GetUtcNow();

        var velocity = await RecordObservationsAsync(ruleSet.OrganizationId, transaction, parties, rulesToEvaluate, now, stoppingToken);

        var results = rulesToEvaluate.Select(rule =>
        {
            var matched = RuleHelper.Matches(rule.RuleBody.ToModel(), evaluateRuleSetRequest.Payload, rule.MatchPolarity, now, parties, velocity);
            var policy = rule.ToPolicy();

            return new EvaluateRuleResponse
            {
                RuleId = rule.RuleId,
                Rule = rule,
                FriendlyName = rule.FriendlyName,
                Matched = matched,
                Policy = policy,
                Outcome = RuleOutcomeResolver.Resolve(matched ? [policy] : [], threshold),
                TransactionId = transaction.TransactionId,
                ExternalReference = transaction.ExternalReference,
                CaseId = null,
                Case = null,
                Decision = null
            };
        }).ToList();

        var firedResults = results.Where(x => x.Matched).ToList();
        var outcome = RuleOutcomeResolver.Resolve(firedResults.ConvertAll(x => x.Policy), threshold);

        var sandboxTrigger = SandboxTriggerResolver.Resolve(
            modeContext.CurrentOrLive, evaluateRuleSetRequest.Payload, evaluateRuleSetRequest.ExternalReference);

        var createCase = evaluateRuleSetRequest.CreateCase;

        if (sandboxTrigger is { } trigger)
        {
            outcome = SandboxTriggerResolver.Apply(trigger, outcome);
            createCase = SandboxTriggerResolver.ForcesCase(trigger) ? new CreateCase(true) : createCase;
        }

        var createUsageRequest = new CreateUsageRequest
        {
            Resource = new Resource(evaluateRuleSetRequest.RuleSetId.ToString()),
            BillableResource = BillableResource.RuleSetEvaluation,
            OrganizationId = ruleSet.OrganizationId,
            Count = new Count(rulesToEvaluate.Count)
        };

        await usageService.RecordBillableAsync(createUsageRequest, stoppingToken);

        var createRuleEvaluationRequests = results.Select(x => new CreateRuleEvaluationRequest
        {
            OrganizationId = x.Rule!.OrganizationId,
            RuleId = x.RuleId,
            RuleVersionId = x.Rule!.CurrentRuleVersionId,
            TransactionId = transaction.TransactionId,
            Payload = transaction.Payload,
            Matched = x.Matched,
            ConditionMatches = x.Matched ? RuleHelper.Trace(x.Rule!.RuleBody.ToModel(), evaluateRuleSetRequest.Payload, x.Rule!.MatchPolarity, now, parties, velocity) : null
        }).ToList();

        var ruleEvaluations = await ruleEvaluationService.CreateManyAsync(createRuleEvaluationRequests, stoppingToken);

        var existingDecision = await decisionService.FindByTransactionAsync(transaction.TransactionId, stoppingToken);

        var (@case, decision) = await DecideAsync(
            existingDecision,
            outcome,
            createCase,
            ruleSet.OrganizationId,
            transaction,
            ruleEvaluations,
            new Matched(firedResults.Count > 0),
            HoldPolicyResolver.Resolve(firedResults.ConvertAll(x => x.Rule!), rulesToEvaluate.FirstOrDefault()?.Organization, appConfig.Value.DecisionConfig),
            firedResults.ConvertAll(x => x.Rule!),
            parties,
            stoppingToken);

        return new EvaluateRuleSetResponse
        {
            RuleSetId = evaluateRuleSetRequest.RuleSetId,
            RuleSet = ruleSet,
            FriendlyName = ruleSet.FriendlyName,
            Results = results,
            Outcome = outcome,
            TransactionId = transaction.TransactionId,
            ExternalReference = transaction.ExternalReference,
            CaseId = @case?.CaseId,
            Case = @case,
            Decision = decision
        };
    }

    public async Task<EvaluateAllRulesResponse> EvaluateAllAsync(EvaluateRuleSetsRequest evaluateRuleSetsRequest, CancellationToken stoppingToken)
    {
        SandboxTriggerResolver.ThrowIfError(modeContext.CurrentOrLive, evaluateRuleSetsRequest.Payload, evaluateRuleSetsRequest.ExternalReference);

        var ruleSets = await ruleSetRepository.GetAllAsync(evaluateRuleSetsRequest.OrganizationId, stoppingToken);

        await transactionSchemaService.EnforcePayloadAsync(evaluateRuleSetsRequest.OrganizationId, evaluateRuleSetsRequest.Payload, stoppingToken);

        evaluateRuleSetsRequest = evaluateRuleSetsRequest with
        {
            Payload = await TokeniseAsync(evaluateRuleSetsRequest.OrganizationId, evaluateRuleSetsRequest.Payload, stoppingToken)
        };

        var enabledRuleSets = ruleSets.Where(x => x.IsEnabled).ToList();

        var rulesToEvaluate = enabledRuleSets
            .SelectMany(x => x.Rules)
            .Where(x => x.IsEnabled && !x.IsDeleted).ToList();

        await sandboxCreditService.EnforceAsync(
            evaluateRuleSetsRequest.OrganizationId,
            BillableResource.RuleSetEvaluation,
            new Resource(evaluateRuleSetsRequest.OrganizationId.ToString()),
            new Count(rulesToEvaluate.Count),
            stoppingToken);

        var defaultThreshold = new ReviewThreshold(appConfig.Value.RuleConfig.DefaultReviewThreshold);
        var thresholdByRuleSetId = enabledRuleSets.ToDictionary(
            x => x.RuleSetId,
            x => x.ReviewThreshold ?? defaultThreshold);

        var createTransactionRequest = new CreateTransactionRequest
        {
            OrganizationId = evaluateRuleSetsRequest.OrganizationId,
            ExternalReference = evaluateRuleSetsRequest.ExternalReference,
            Payload = evaluateRuleSetsRequest.Payload
        };

        var transaction = await transactionService.GetOrCreateAsync(createTransactionRequest, stoppingToken);

        var parties = await RecordPartiesAsync(evaluateRuleSetsRequest.OrganizationId, transaction, stoppingToken);

        var now = timeProvider.GetUtcNow();

        var velocity = await RecordObservationsAsync(evaluateRuleSetsRequest.OrganizationId, transaction, parties, rulesToEvaluate, now, stoppingToken);

        var results = rulesToEvaluate.Select(rule =>
        {
            var matched = RuleHelper.Matches(rule.RuleBody.ToModel(), evaluateRuleSetsRequest.Payload, rule.MatchPolarity, now, parties, velocity);
            var policy = rule.ToPolicy();
            var ruleThreshold = thresholdByRuleSetId.GetValueOrDefault(rule.RuleSetId, defaultThreshold);

            return new EvaluateRuleResponse
            {
                RuleId = rule.RuleId,
                Rule = rule,
                FriendlyName = rule.FriendlyName,
                Matched = matched,
                Policy = policy,
                Outcome = RuleOutcomeResolver.Resolve(matched ? [policy] : [], ruleThreshold),
                TransactionId = transaction.TransactionId,
                ExternalReference = transaction.ExternalReference,
                CaseId = null,
                Case = null,
                Decision = null
            };
        }).ToList();

        var firedResults = results.Where(x => x.Matched).ToList();

        var threshold = firedResults.Count > 0
            ? new ReviewThreshold(firedResults.Max(x => (int)thresholdByRuleSetId.GetValueOrDefault(x.Rule!.RuleSetId, defaultThreshold)))
            : defaultThreshold;

        var outcome = RuleOutcomeResolver.Resolve(firedResults.Select(x => x.Policy).ToList(), threshold);

        var sandboxTrigger = SandboxTriggerResolver.Resolve(
            modeContext.CurrentOrLive, evaluateRuleSetsRequest.Payload, evaluateRuleSetsRequest.ExternalReference);

        var createCase = evaluateRuleSetsRequest.CreateCase;

        if (sandboxTrigger is { } trigger)
        {
            outcome = SandboxTriggerResolver.Apply(trigger, outcome);
            createCase = SandboxTriggerResolver.ForcesCase(trigger) ? new CreateCase(true) : createCase;
        }

        var createUsageRequest = new CreateUsageRequest
        {
            Resource = new Resource(evaluateRuleSetsRequest.OrganizationId.ToString()),
            BillableResource = BillableResource.RuleSetEvaluation,
            OrganizationId = evaluateRuleSetsRequest.OrganizationId,
            Count = new Count(rulesToEvaluate.Count)
        };

        await usageService.RecordBillableAsync(createUsageRequest, stoppingToken);

        var createRuleEvaluationRequests = results.Select(x => new CreateRuleEvaluationRequest
        {
            OrganizationId = x.Rule!.OrganizationId,
            RuleId = x.RuleId,
            RuleVersionId = x.Rule!.CurrentRuleVersionId,
            TransactionId = transaction.TransactionId,
            Payload = transaction.Payload,
            Matched = x.Matched,
            ConditionMatches = x.Matched ? RuleHelper.Trace(x.Rule!.RuleBody.ToModel(), evaluateRuleSetsRequest.Payload, x.Rule!.MatchPolarity, now, parties, velocity) : null
        }).ToList();

        var ruleEvaluations = await ruleEvaluationService.CreateManyAsync(createRuleEvaluationRequests, stoppingToken);

        var existingDecision = await decisionService.FindByTransactionAsync(transaction.TransactionId, stoppingToken);

        var (@case, decision) = await DecideAsync(
            existingDecision,
            outcome,
            createCase,
            evaluateRuleSetsRequest.OrganizationId,
            transaction,
            ruleEvaluations,
            new Matched(firedResults.Count > 0),
            HoldPolicyResolver.Resolve(firedResults.ConvertAll(x => x.Rule!), rulesToEvaluate.FirstOrDefault()?.Organization, appConfig.Value.DecisionConfig),
            firedResults.ConvertAll(x => x.Rule!),
            parties,
            stoppingToken);

        return new EvaluateAllRulesResponse
        {
            Results = results,
            Outcome = outcome,
            TransactionId = transaction.TransactionId,
            ExternalReference = transaction.ExternalReference,
            CaseId = @case?.CaseId,
            Case = @case,
            Decision = decision
        };
    }

    private Task<List<PartyFacts>> RecordPartiesAsync(OrganizationId organizationId, Transaction transaction, CancellationToken stoppingToken)
    {
        return partyService.RecordAsync(new RecordTransactionPartiesRequest
        {
            OrganizationId = organizationId,
            TransactionId = transaction.TransactionId,
            Payload = transaction.Payload
        }, stoppingToken);
    }

    private Task<VelocityFacts> RecordObservationsAsync(
        OrganizationId organizationId,
        Transaction transaction,
        IReadOnlyList<PartyFacts> parties,
        List<Rule> rules,
        DateTimeOffset now,
        CancellationToken stoppingToken)
    {
        return velocityService.RecordAndReadAsync(new RecordVelocityObservationsRequest
        {
            OrganizationId = organizationId,
            TransactionId = transaction.TransactionId,
            ObservedAt = new ObservedAt(transaction.CreatedAt),
            Payload = transaction.Payload,
            Parties = parties
        }, rules.ConvertAll(x => x.RuleBody.ToModel()), new EvaluatedAt(now), stoppingToken);
    }

    private async Task<AlertDraft> PrepareAlertsAsync(
        Decision? existingDecision,
        RuleOutcome outcome,
        CreateCase createCase,
        OrganizationId organizationId,
        TransactionId transactionId,
        List<Rule> firedRules,
        List<RuleEvaluation> ruleEvaluations,
        IReadOnlyList<PartyFacts> parties,
        CancellationToken stoppingToken)
    {
        if (firedRules.Count == 0 || existingDecision is not null || !outcome.Action.OpensCase() || !createCase)
        {
            return EmptyAlertDraft();
        }

        var evaluationByRuleId = ruleEvaluations.ToDictionary(x => x.RuleId);
        var candidates = new List<AlertCandidate>();

        foreach (var rule in firedRules)
        {
            if (!evaluationByRuleId.TryGetValue(rule.RuleId, out var ruleEvaluation))
            {
                continue;
            }

            var alertPolicy = AlertPolicyResolver.Resolve(rule, rule.Organization, appConfig.Value.AlertConfig);
            var subject = parties.FirstOrDefault(x => x.Role == alertPolicy.SubjectRole);

            candidates.Add(new AlertCandidate
            {
                RuleId = rule.RuleId,
                RuleVersionId = ruleEvaluation.RuleVersionId,
                RuleEvaluationId = ruleEvaluation.RuleEvaluationId,
                Severity = rule.Severity,
                PartyId = subject?.PartyId,
                PartyRole = subject?.Role,
                AlertPolicy = alertPolicy
            });
        }

        if (candidates.Count == 0)
        {
            return EmptyAlertDraft();
        }

        return await alertService.PrepareAsync(new RaiseAlertsRequest
        {
            OrganizationId = organizationId,
            TransactionId = transactionId,
            RaisedAt = new RaisedAt(timeProvider.GetUtcNow()),
            Candidates = candidates
        }, stoppingToken);
    }

    private static AlertDraft EmptyAlertDraft()
    {
        return new AlertDraft
        {
            Alerts = [],
            CaseId = null,
            AllSuppressed = new AllSuppressed(false)
        };
    }

    private async Task<Case?> OpenCaseAsync(
        Decision? existingDecision,
        RuleOutcome outcome,
        CreateCase createCase,
        OrganizationId organizationId,
        Transaction transaction,
        List<RuleEvaluation> ruleEvaluations,
        AlertDraft alerts,
        HoldPolicy holdPolicy,
        IReadOnlyList<PartyFacts> parties,
        CancellationToken stoppingToken)
    {
        if (existingDecision is not null || !outcome.Action.OpensCase() || !createCase)
        {
            return null;
        }

        if (alerts.CaseId.HasValue)
        {
            return await caseService.GetAsync(alerts.CaseId.Value, stoppingToken);
        }

        if (alerts.AllSuppressed)
        {
            return null;
        }

        var createCaseRequest = new CreateCaseRequest
        {
            OrganizationId = organizationId,
            Severity = outcome.Severity,
            PartyId = alerts.Alerts.Find(x => x.Status is AlertStatus.Open && x.PartyId.HasValue)?.PartyId,
            TransactionId = transaction.TransactionId,
            RuleEvaluationIds = ruleEvaluations.Where(x => x.Matched).Select(x => x.RuleEvaluationId).ToList(),
            RuleIds = ruleEvaluations.Where(x => x.Matched).Select(x => x.RuleId).ToList(),
            Payload = transaction.Payload,
            Parties = [..parties],

            HoldPolicy = outcome.Action is RuleAction.Review ? holdPolicy : null
        };

        return await caseService.CreateAsync(createCaseRequest, stoppingToken);
    }

    private async Task<(Case? Case, Decision Decision)> DecideAsync(
        Decision? existingDecision,
        RuleOutcome outcome,
        CreateCase createCase,
        OrganizationId organizationId,
        Transaction transaction,
        List<RuleEvaluation> ruleEvaluations,
        Matched anyMatched,
        HoldPolicy holdPolicy,
        List<Rule> firedRules,
        IReadOnlyList<PartyFacts> parties,
        CancellationToken stoppingToken)
    {
        try
        {
            return await unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var alerts = await PrepareAlertsAsync(existingDecision, outcome, createCase, organizationId, transaction.TransactionId, firedRules, ruleEvaluations, parties, stoppingToken);

                var @case = await OpenCaseAsync(existingDecision, outcome, createCase, organizationId, transaction, ruleEvaluations, alerts, holdPolicy, parties, stoppingToken);

                await alertService.RaiseAsync(alerts.Alerts, @case?.CaseId ?? alerts.CaseId, stoppingToken);

                var decision = await RecordDecisionAsync(organizationId, transaction.TransactionId, outcome, anyMatched, holdPolicy, @case?.CaseId, ruleEvaluations, stoppingToken);

                return (@case, decision);
            }, stoppingToken);
        }
        catch (DecisionAlreadyRecordedException)
        {
            var decision = await decisionService.FindByTransactionAsync(transaction.TransactionId, stoppingToken);
            var @case = decision!.CaseId.HasValue ? await caseService.GetAsync(decision.CaseId.Value, stoppingToken) : null;

            return (@case, decision);
        }
    }

    private Task<Decision> RecordDecisionAsync(
        OrganizationId organizationId,
        TransactionId transactionId,
        RuleOutcome outcome,
        Matched anyMatched,
        HoldPolicy holdPolicy,
        CaseId? caseId,
        List<RuleEvaluation> ruleEvaluations,
        CancellationToken stoppingToken)
    {
        var recordDecisionRequest = new RecordDecisionRequest
        {
            OrganizationId = organizationId,
            TransactionId = transactionId,
            Outcome = outcome,
            AnyMatched = anyMatched,
            HoldPolicy = holdPolicy,
            CaseId = caseId,
            RuleEvaluationIds = ruleEvaluations.ConvertAll(x => x.RuleEvaluationId)
        };

        return decisionService.RecordAsync(recordDecisionRequest, stoppingToken);
    }

    private async Task<JsonObject> TokeniseAsync(OrganizationId organizationId, JsonObject payload, CancellationToken stoppingToken)
    {
        var dataKey = await payloadProtectionService.GetOrCreateDataKeyAsync(organizationId, stoppingToken);

        return PayloadTokeniser.Tokenise(dataKey, payload);
    }
}
