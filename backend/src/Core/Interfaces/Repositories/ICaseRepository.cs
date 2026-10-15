using System.Text.Json.Nodes;
using Core.Enums;
using Core.Models.Cases;
using Core.Models.Escalations;
using Core.Models.Queues;
using Core.Models.Statistics;
using Core.ValueObjects;

namespace Core.Interfaces.Repositories;

public interface ICaseRepository : IPagedRepository<ListCaseRequest, Case>
{
    Task AddAsync(Case @case, CancellationToken stoppingToken);
    Task<Exists> ExistsAsync(OrganizationId organizationId, CaseId caseId, IncludeDeleted includeDeleted, CancellationToken stoppingToken);
    Task<Case> GetAsync(CaseId caseId, CancellationToken stoppingToken);
    Task AssignAsync(CaseId caseId, ApplicationUserId userId, CancellationToken stoppingToken);

    // Owns its own save: concurrent claims are settled by whichever write lands first, so the retry lives here.
    Task<Case?> ClaimNextAsync(ClaimNextRequest claimNextRequest, CancellationToken stoppingToken);
    Task UnassignAsync(CaseId caseId, CancellationToken stoppingToken);

    Task<Count> ReassignOpenCasesAsync(
        OrganizationId organizationId, ApplicationUserId fromUserId, ApplicationUserId? toUserId, CancellationToken stoppingToken);
    Task<Case> ResolveAsync(CaseId caseId, CaseClosure caseClosure, CancellationToken stoppingToken);
    Task<Case> ReopenAsync(CaseId caseId, CancellationToken stoppingToken);
    Task<Case> ChangeStatusAsync(CaseId caseId, CaseStatus status, CancellationToken stoppingToken);
    Task<Dictionary<CaseStatus, TotalCount>> CountByStatusAsync(ListCaseRequest listCaseRequest, CancellationToken stoppingToken);
    Task<List<Case>> ExpireManyAsync(IReadOnlyCollection<CaseId> caseIds, CancellationToken stoppingToken);
    Task DeleteAsync(CaseId caseId, CancellationToken stoppingToken);
    Task RecoverAsync(CaseId caseId, CancellationToken stoppingToken);
    Task<FriendlyName> GenerateFriendlyName(CancellationToken stoppingToken);
    Task<TimeSpan?> GetAverageResolutionTimeAsync(StatisticRequest statisticRequest, CancellationToken stoppingToken);
    Task<TotalCount> CountBreachedAsync(ListCaseRequest listCaseRequest, CancellationToken stoppingToken);

    Task<List<EscalationCandidate>> ListEscalationCandidatesAsync(ListEscalationCandidateRequest listEscalationCandidateRequest, CancellationToken stoppingToken);

    Task<List<Case>> ListBreachNotificationCandidatesAsync(Count batchSize, CancellationToken stoppingToken);
    Task MarkBreachNotifiedAsync(List<CaseId> caseIds, CancellationToken stoppingToken);
    Task<List<Case>> EscalateManyAsync(IReadOnlyCollection<CaseEscalation> caseEscalations, CancellationToken stoppingToken);

    Task<JsonObject> GetPayloadAsync(CaseId caseId, CancellationToken stoppingToken);

    Task<EvidencePackSource> GetEvidencePackSourceAsync(CaseId caseId, CancellationToken stoppingToken);
}