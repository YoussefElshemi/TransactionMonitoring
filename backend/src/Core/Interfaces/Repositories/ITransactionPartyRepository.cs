using Core.Models.Parties;
using Core.ValueObjects;

namespace Core.Interfaces.Repositories;

public interface ITransactionPartyRepository
{
    Task AppendAsync(List<TransactionParty> transactionParties, CancellationToken stoppingToken);
    Task<List<TransactionParty>> ListByTransactionAsync(TransactionId transactionId, CancellationToken stoppingToken);
}
