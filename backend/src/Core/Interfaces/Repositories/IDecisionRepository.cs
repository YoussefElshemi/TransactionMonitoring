using Core.Enums;
using Core.Models.Decisions;
using Core.ValueObjects;

namespace Core.Interfaces.Repositories;

public interface IDecisionRepository
{
    Task AddAsync(Decision decision, CancellationToken stoppingToken);
    Task<Decision> GetAsync(DecisionId decisionId, CancellationToken stoppingToken);
    Task<Decision?> FindAsync(DecisionId decisionId, CancellationToken stoppingToken);
    Task<Decision?> FindByTransactionAsync(TransactionId transactionId, CancellationToken stoppingToken);
    Task<List<Decision>> ResolveByCaseAsync(ResolveDecisionRequest resolveDecisionRequest, CancellationToken stoppingToken);
    Task<Decision?> ResolveAsync(DecisionId decisionId, DecisionVerdict verdict, ApplicationUserId resolvedByUserId, CancellationToken stoppingToken);
    Task<Exists> ExistsAsync(OrganizationId organizationId, DecisionId decisionId, CancellationToken stoppingToken);
    Task<List<Decision>> ListDueForExpiryAsync(int batchSize, CancellationToken stoppingToken);

    // Excludes decisions being settled now: they are modified in the change tracker but unflushed, so a query would read stale status.
    Task<Dictionary<CaseId, List<DecisionStatus>>> ListStatusesByCaseAsync(
        IReadOnlyCollection<CaseId> caseIds,
        IReadOnlyCollection<DecisionId> settlingDecisionIds,
        CancellationToken stoppingToken);
    Task<List<Decision>> ExpireManyAsync(IReadOnlyDictionary<DecisionId, DecisionVerdict> verdicts, CancellationToken stoppingToken);
}
