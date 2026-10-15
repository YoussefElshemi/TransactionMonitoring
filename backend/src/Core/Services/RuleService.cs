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
using Core.Models.RuleDeployments;
using Core.Models.RuleEvaluations;
using Core.Models.Rules;
using Core.Models.Transactions;
using Core.Models.Usages;
using Core.Models.VelocityObservations;
using Core.ValueObjects;
using Microsoft.Extensions.Options;

namespace Core.Services;

public class RuleService(
    IRuleRepository ruleRepository,
    IRuleVersionRepository ruleVersionRepository,
    IRuleDeploymentRepository ruleDeploymentRepository,
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
    IPartyMappingService partyMappingService,
    IVelocityService velocityService,
    IVelocityProfileService velocityProfileService,
    IUnitOfWork unitOfWork,
    IRandomProvider randomProvider,
    IOptions<AppConfig> appConfig,
    TimeProvider timeProvider,
    ISandboxCreditService sandboxCreditService,
    IModeContext modeContext) : IRuleService
{
    private const string TestResource = "Test";

    public async Task<Rule> CreateAsync(CreateRuleRequest createRuleRequest, CancellationToken stoppingToken)
    {
        await transactionSchemaService.EnforceRuleBodyAsync(createRuleRequest.OrganizationId, createRuleRequest.RuleBody.ToModel(), stoppingToken);
        await partyMappingService.EnforceRuleBodyAsync(createRuleRequest.OrganizationId, createRuleRequest.RuleBody.ToModel(), stoppingToken);
        await velocityProfileService.EnforceRuleBodyAsync(createRuleRequest.OrganizationId, createRuleRequest.RuleBody.ToModel(), stoppingToken);

        var ruleVersionId = new RuleVersionId(Guid.NewGuid());
        var rule = createRuleRequest.ToModel(ruleVersionId, timeProvider, modeContext.CurrentOrLive);

        await ruleRepository.AddAsync(rule, stoppingToken);

        var ruleVersion = rule.ToVersion(ruleVersionId, rule.CurrentVersionNumber, rule.CreatedByUserId, timeProvider, modeContext.CurrentOrLive);
        await ruleVersionRepository.AddAsync(ruleVersion, stoppingToken);

        var now = timeProvider.GetUtcNow();
        var deployment = new RuleDeployment
        {
            Mode = modeContext.CurrentOrLive,
            RuleDeploymentId = new RuleDeploymentId(Guid.NewGuid()),
            OrganizationId = rule.OrganizationId,
            RuleId = rule.RuleId,
            TargetRuleVersionId = ruleVersion.RuleVersionId,
            PreviousRuleVersionId = null,
            Strategy = DeploymentStrategy.AllAtOnce,
            Status = RuleDeploymentStatus.Completed,
            CurrentPercentage = new RolloutPercentage(100),
            StepPercentage = null,
            StepIntervalSeconds = null,
            CreatedByUserId = rule.CreatedByUserId,
            CompletedAt = new CompletedAt(now),
            CreatedAt = new CreatedAt(now),
            UpdatedAt = new UpdatedAt(now)
        };
        await ruleDeploymentRepository.AddAsync(deployment, stoppingToken);

        await unitOfWork.SaveChangesAsync(stoppingToken);

        return await GetAsync(rule.RuleId, stoppingToken);
    }

    public async Task<Rule> UpdateAsync(UpdateRuleRequest updateRuleRequest, CancellationToken stoppingToken)
    {
        await ruleRepository.UpdateAsync(updateRuleRequest, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);

        return await GetAsync(updateRuleRequest.RuleId, stoppingToken);
    }

    public Task<Exists> ExistsAsync(OrganizationId organizationId, RuleId ruleId, IncludeDeleted includeDeleted, CancellationToken stoppingToken)
    {
        return ruleRepository.ExistsAsync(organizationId, ruleId, includeDeleted, stoppingToken);
    }

    public Task<PagedResponse<Rule>> ListAsync(ListRuleRequest listRuleRequest, CancellationToken stoppingToken)
    {
        return ruleRepository.ListAsync(listRuleRequest, stoppingToken);
    }

    public Task<Rule> GetAsync(RuleId ruleId, CancellationToken stoppingToken)
    {
        return ruleRepository.GetAsync(ruleId, stoppingToken);
    }

    public Task<List<Rule>> ListByIdsAsync(IReadOnlyCollection<RuleId> ruleIds, CancellationToken stoppingToken)
    {
        return ruleRepository.ListByIdsAsync(ruleIds, stoppingToken);
    }

    public async Task DeleteAsync(RuleId ruleId, CancellationToken stoppingToken)
    {
        await ruleRepository.DeleteAsync(ruleId, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);
    }

    public async Task RecoverAsync(RuleId ruleId, CancellationToken stoppingToken)
    {
        await ruleRepository.RecoverAsync(ruleId, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);
    }

    public async Task<EvaluateRuleResponse> EvaluateAsync(EvaluateRuleRequest evaluateRuleRequest, CancellationToken stoppingToken)
    {
        SandboxTriggerResolver.ThrowIfError(modeContext.CurrentOrLive, evaluateRuleRequest.Payload, evaluateRuleRequest.ExternalReference);

        var rule = await ruleRepository.GetAsync(evaluateRuleRequest.RuleId, stoppingToken);

        await sandboxCreditService.EnforceAsync(
            rule.OrganizationId, BillableResource.RuleEvaluation, new Resource(rule.RuleId.ToString()), new Count(1), stoppingToken);

        await transactionSchemaService.EnforcePayloadAsync(rule.OrganizationId, evaluateRuleRequest.Payload, stoppingToken);

        evaluateRuleRequest = evaluateRuleRequest with
        {
            Payload = PayloadTokeniser.Tokenise(
                await payloadProtectionService.GetOrCreateDataKeyAsync(rule.OrganizationId, stoppingToken),
                evaluateRuleRequest.Payload)
        };

        var ruleBody = rule.RuleBody;
        var ruleVersionId = rule.CurrentRuleVersionId;
        var matchPolarity = rule.MatchPolarity;
        var policy = rule.ToPolicy();

        if (rule.HasActiveDeployment)
        {
            var activeDeployment = await ruleDeploymentRepository.GetActiveByRuleIdAsync(rule.RuleId, stoppingToken);

            if (activeDeployment is { Strategy: DeploymentStrategy.Gradual } &&
                randomProvider.Next(100) < activeDeployment.CurrentPercentage)
            {
                var targetVersion = await ruleVersionRepository.GetAsync(activeDeployment.TargetRuleVersionId, stoppingToken);
                ruleBody = targetVersion.RuleBody;
                ruleVersionId = targetVersion.RuleVersionId;
                matchPolarity = targetVersion.MatchPolarity;
                policy = targetVersion.ToPolicy();
            }
        }

        var createTransactionRequest = new CreateTransactionRequest
        {
            OrganizationId = rule.OrganizationId,
            ExternalReference = evaluateRuleRequest.ExternalReference,
            Payload = evaluateRuleRequest.Payload
        };

        var transaction = await transactionService.GetOrCreateAsync(createTransactionRequest, stoppingToken);

        var parties = await partyService.RecordAsync(new RecordTransactionPartiesRequest
        {
            OrganizationId = rule.OrganizationId,
            TransactionId = transaction.TransactionId,
            Payload = transaction.Payload
        }, stoppingToken);

        var ruleBodyModel = ruleBody.ToModel();
        var now = timeProvider.GetUtcNow();

        var velocity = await velocityService.RecordAndReadAsync(new RecordVelocityObservationsRequest
        {
            OrganizationId = rule.OrganizationId,
            TransactionId = transaction.TransactionId,
            ObservedAt = new ObservedAt(transaction.CreatedAt),
            Payload = transaction.Payload,
            Parties = parties
        }, [ruleBodyModel], new EvaluatedAt(now), stoppingToken);

        var matched = RuleHelper.Matches(ruleBodyModel, evaluateRuleRequest.Payload, matchPolarity, now, parties, velocity);

        var reviewThreshold = await ruleSetRepository.GetReviewThresholdAsync(rule.RuleSetId, stoppingToken);
        var threshold = reviewThreshold ?? new ReviewThreshold(appConfig.Value.RuleConfig.DefaultReviewThreshold);
        var outcome = RuleOutcomeResolver.Resolve(matched ? [policy] : [], threshold);

        var sandboxTrigger = SandboxTriggerResolver.Resolve(
            modeContext.CurrentOrLive, evaluateRuleRequest.Payload, evaluateRuleRequest.ExternalReference);

        var createCase = evaluateRuleRequest.CreateCase;

        if (sandboxTrigger is { } trigger)
        {
            outcome = SandboxTriggerResolver.Apply(trigger, outcome);
            createCase = SandboxTriggerResolver.ForcesCase(trigger) ? new CreateCase(true) : createCase;
        }

        var createUsageRequest = new CreateUsageRequest
        {
            Resource = new Resource(rule.RuleId.ToString()),
            BillableResource = BillableResource.RuleEvaluation,
            OrganizationId = rule.OrganizationId,
            Count = new Count(1)
        };

        await usageService.RecordBillableAsync(createUsageRequest, stoppingToken);

        var createRuleEvaluationRequest = new CreateRuleEvaluationRequest
        {
            OrganizationId = rule.OrganizationId,
            RuleId = rule.RuleId,
            RuleVersionId = ruleVersionId,
            TransactionId = transaction.TransactionId,
            Payload = transaction.Payload,
            Matched = matched,
            ConditionMatches = matched ? RuleHelper.Trace(ruleBodyModel, evaluateRuleRequest.Payload, matchPolarity, now, parties, velocity) : null
        };

        var ruleEvaluation = await ruleEvaluationService.CreateAsync(createRuleEvaluationRequest, stoppingToken);

        var existingDecision = await decisionService.FindByTransactionAsync(transaction.TransactionId, stoppingToken);

        var (@case, decision) = await DecideAsync(
            existingDecision,
            outcome,
            createCase,
            rule,
            policy,
            transaction,
            ruleEvaluation,
            matched,
            parties,
            stoppingToken);

        return new EvaluateRuleResponse
        {
            RuleId = rule.RuleId,
            Rule = rule,
            FriendlyName = rule.FriendlyName,
            Matched = matched,
            Policy = policy,
            Outcome = outcome,
            TransactionId = transaction.TransactionId,
            ExternalReference = transaction.ExternalReference,
            CaseId = @case?.CaseId,
            Case = @case,
            Decision = decision
        };
    }

    private async Task<AlertDraft> PrepareAlertsAsync(
        Decision? existingDecision,
        RuleOutcome outcome,
        CreateCase createCase,
        Rule rule,
        RulePolicy policy,
        Transaction transaction,
        RuleEvaluation ruleEvaluation,
        Matched matched,
        IReadOnlyList<PartyFacts> parties,
        CancellationToken stoppingToken)
    {
        if (!matched || existingDecision is not null || !outcome.Action.OpensCase() || !createCase)
        {
            return EmptyAlertDraft();
        }

        var alertPolicy = AlertPolicyResolver.Resolve(rule, rule.Organization, appConfig.Value.AlertConfig);
        var subject = parties.FirstOrDefault(x => x.Role == alertPolicy.SubjectRole);

        var candidate = new AlertCandidate
        {
            RuleId = rule.RuleId,
            RuleVersionId = ruleEvaluation.RuleVersionId,
            RuleEvaluationId = ruleEvaluation.RuleEvaluationId,
            Severity = policy.Severity,
            PartyId = subject?.PartyId,
            PartyRole = subject?.Role,
            AlertPolicy = alertPolicy
        };

        return await alertService.PrepareAsync(new RaiseAlertsRequest
        {
            OrganizationId = rule.OrganizationId,
            TransactionId = transaction.TransactionId,
            RaisedAt = new RaisedAt(timeProvider.GetUtcNow()),
            Candidates = [candidate]
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
        Rule rule,
        Transaction transaction,
        RuleEvaluation ruleEvaluation,
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
            OrganizationId = rule.OrganizationId,
            Severity = outcome.Severity,
            PartyId = alerts.Alerts.Find(x => x.Status is AlertStatus.Open && x.PartyId.HasValue)?.PartyId,
            TransactionId = transaction.TransactionId,
            RuleEvaluationIds = [ruleEvaluation.RuleEvaluationId],
            RuleIds = [rule.RuleId],
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
        Rule rule,
        RulePolicy policy,
        Transaction transaction,
        RuleEvaluation ruleEvaluation,
        Matched matched,
        IReadOnlyList<PartyFacts> parties,
        CancellationToken stoppingToken)
    {
        try
        {
            return await unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var alerts = await PrepareAlertsAsync(existingDecision, outcome, createCase, rule, policy, transaction, ruleEvaluation, matched, parties, stoppingToken);

                var holdPolicy = HoldPolicyResolver.Resolve(matched ? [rule] : [], rule.Organization, appConfig.Value.DecisionConfig);

                var @case = await OpenCaseAsync(existingDecision, outcome, createCase, rule, transaction, ruleEvaluation, alerts, holdPolicy, parties, stoppingToken);

                await alertService.RaiseAsync(alerts.Alerts, @case?.CaseId ?? alerts.CaseId, stoppingToken);

                var recordDecisionRequest = new RecordDecisionRequest
                {
                    OrganizationId = rule.OrganizationId,
                    TransactionId = transaction.TransactionId,
                    Outcome = outcome,
                    AnyMatched = matched,
                    HoldPolicy = holdPolicy,
                    CaseId = @case?.CaseId,
                    RuleEvaluationIds = [ruleEvaluation.RuleEvaluationId]
                };

                return (@case, await decisionService.RecordAsync(recordDecisionRequest, stoppingToken));
            }, stoppingToken);
        }
        catch (DecisionAlreadyRecordedException)
        {
            return await GetRecordedDecisionAsync(transaction.TransactionId, stoppingToken);
        }
    }

    private async Task<(Case? Case, Decision Decision)> GetRecordedDecisionAsync(TransactionId transactionId, CancellationToken stoppingToken)
    {
        var decision = await decisionService.FindByTransactionAsync(transactionId, stoppingToken);
        var @case = decision!.CaseId.HasValue ? await caseService.GetAsync(decision.CaseId.Value, stoppingToken) : null;

        return (@case, decision);
    }

    public async Task<TestRuleResponse> TestAsync(TestRuleRequest testRuleRequest, CancellationToken stoppingToken)
    {
        await sandboxCreditService.EnforceAsync(
            testRuleRequest.OrganizationId, BillableResource.RuleTest, new Resource(TestResource), new Count(1), stoppingToken);

        var ruleBody = testRuleRequest.RuleBody.ToModel();

        await transactionSchemaService.EnforceAsync(testRuleRequest.OrganizationId, ruleBody, testRuleRequest.Payload, stoppingToken);
        await partyMappingService.EnforceRuleBodyAsync(testRuleRequest.OrganizationId, ruleBody, stoppingToken);
        await velocityProfileService.EnforceRuleBodyAsync(testRuleRequest.OrganizationId, ruleBody, stoppingToken);

        testRuleRequest = testRuleRequest with
        {
            Payload = PayloadTokeniser.Tokenise(
                await payloadProtectionService.GetOrCreateDataKeyAsync(testRuleRequest.OrganizationId, stoppingToken),
                testRuleRequest.Payload)
        };

        var parties = await partyService.PeekAsync(testRuleRequest.OrganizationId, testRuleRequest.Payload, stoppingToken);

        var now = timeProvider.GetUtcNow();

        var velocityFacts = await velocityService.PeekManyAsync(testRuleRequest.OrganizationId, [new VelocityPeekSample
        {
            Payload = testRuleRequest.Payload,
            Parties = parties,
            EvaluatedAt = new EvaluatedAt(now),
            TransactionId = null
        }], [ruleBody], stoppingToken);

        var matched = RuleHelper.Matches(ruleBody, testRuleRequest.Payload, testRuleRequest.MatchPolarity, now, parties, velocityFacts[0]);
        var policy = testRuleRequest.ToPolicy();
        var threshold = new ReviewThreshold(appConfig.Value.RuleConfig.DefaultReviewThreshold);

        var createUsageRequest = new CreateUsageRequest
        {
            Resource = new Resource(TestResource),
            BillableResource = BillableResource.RuleTest,
            OrganizationId = testRuleRequest.OrganizationId,
            Count = new Count(1)
        };

        await usageService.RecordBillableAsync(createUsageRequest, stoppingToken);

        return new TestRuleResponse
        {
            Matched = matched,
            Policy = policy,
            Outcome = RuleOutcomeResolver.Resolve(matched ? [policy] : [], threshold)
        };
    }

    public async Task MoveAsync(RuleId ruleId, RuleSetId targetRuleSetId, CancellationToken stoppingToken)
    {
        await ruleRepository.MoveAsync(ruleId, targetRuleSetId, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);
    }

    public async Task<Rule> CopyRuleAsync(CopyRuleRequest copyRuleRequest, CancellationToken stoppingToken)
    {
        var rule = await GetAsync(copyRuleRequest.RuleId, stoppingToken);

        var createRuleRequest = new CreateRuleRequest
        {
            FriendlyName = rule.FriendlyName,
            RuleBody = rule.RuleBody,
            MatchPolarity = rule.MatchPolarity,
            Action = rule.Action,
            Severity = rule.Severity,
            RiskWeight = rule.RiskWeight,
            HoldWindow = rule.HoldWindow,
            HoldExpiryVerdict = rule.HoldExpiryVerdict,
            AlertSubjectRole = rule.AlertSubjectRole,
            DeduplicationWindow = rule.DeduplicationWindow,
            OrganizationId = rule.OrganizationId,
            IsEnabled = rule.IsEnabled,
            RuleSetId = copyRuleRequest.TargetRuleSetId,
            CreatedByUserId = copyRuleRequest.CreatedByUserId
        };

        return await CreateAsync(createRuleRequest, stoppingToken);
    }

    public Task<TotalCount> GetCountAsync(ListRuleRequest listRuleRequest, CancellationToken stoppingToken)
    {
        return ruleRepository.GetCountAsync(listRuleRequest, stoppingToken);
    }
}
