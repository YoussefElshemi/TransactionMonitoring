using Core.Models.Transactions;
using Core.ValueObjects;

namespace Core.Interfaces.Services;

public interface ITransactionService : IPagedService<ListTransactionRequest, Transaction>
{
    Task<Transaction> GetOrCreateAsync(CreateTransactionRequest createTransactionRequest, CancellationToken stoppingToken);
    Task<Transaction> GetAsync(TransactionId transactionId, CancellationToken stoppingToken);
    Task<List<Transaction>> ListByIdsAsync(IReadOnlyCollection<TransactionId> transactionIds, CancellationToken stoppingToken);
    Task<Exists> ExistsAsync(OrganizationId organizationId, TransactionId transactionId, IncludeDeleted includeDeleted, CancellationToken stoppingToken);
}
