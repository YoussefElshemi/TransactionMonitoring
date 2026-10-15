using Core.Models.Transactions;
using Core.ValueObjects;

namespace Core.Interfaces.Repositories;

public interface ITransactionRepository : IPagedRepository<ListTransactionRequest, Transaction>
{
    Task<Transaction?> TryAddAsync(Transaction transaction, CancellationToken stoppingToken);
    Task<Exists> ExistsAsync(OrganizationId organizationId, TransactionId transactionId, IncludeDeleted includeDeleted, CancellationToken stoppingToken);
    Task<Transaction> GetAsync(TransactionId transactionId, CancellationToken stoppingToken);
    Task<List<Transaction>> ListByIdsAsync(IReadOnlyCollection<TransactionId> transactionIds, CancellationToken stoppingToken);
    Task<Transaction?> FindByExternalReferenceAsync(OrganizationId organizationId, ExternalReference externalReference, CancellationToken stoppingToken);
    Task<FriendlyName> GenerateFriendlyName(CancellationToken stoppingToken);
}
