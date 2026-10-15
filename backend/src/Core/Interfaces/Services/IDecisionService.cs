using Core.Models.Decisions;
using Core.ValueObjects;

namespace Core.Interfaces.Services;

public interface IDecisionService
{
    Task<Decision> RecordAsync(RecordDecisionRequest recordDecisionRequest, CancellationToken stoppingToken);

    Task<Decision?> FindByTransactionAsync(TransactionId transactionId, CancellationToken stoppingToken);

    Task<Count> ExpireDueAsync(CancellationToken stoppingToken);

    Task ResolveAsync(ResolveDecisionByIdRequest resolveDecisionByIdRequest, CancellationToken stoppingToken);

    Task<Exists> SettlesCaseAsync(DecisionId decisionId, CancellationToken stoppingToken);

    Task<Exists> ExistsAsync(OrganizationId organizationId, DecisionId decisionId, CancellationToken stoppingToken);
}
