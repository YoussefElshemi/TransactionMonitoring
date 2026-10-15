using Core.Interfaces.Repositories;
using Core.Models.Parties;
using Core.ValueObjects;
using Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class TransactionPartyRepository(
    TransactionMonitoringDbContext context) : ITransactionPartyRepository
{
    public Task AppendAsync(List<TransactionParty> transactionParties, CancellationToken stoppingToken)
    {
        if (transactionParties.Count == 0)
        {
            return Task.CompletedTask;
        }

        var transactionIds = transactionParties.Select(x => (Guid)x.TransactionId).Distinct().ToList();

        return IdempotentAppender.AppendAsync(
            context,
            transactionParties.ConvertAll(x => x.ToEntity()),
            async ct => (await context.TransactionParties
                    .Where(x => transactionIds.Contains(x.TransactionId))
                    .Select(x => new { x.TransactionId, x.PartyId, x.PartyRoleId })
                    .ToListAsync(ct))
                .ConvertAll(x => (x.TransactionId, x.PartyId, x.PartyRoleId)),
            x => (x.TransactionId, x.PartyId, x.PartyRoleId),
            stoppingToken);
    }

    public async Task<List<TransactionParty>> ListByTransactionAsync(TransactionId transactionId, CancellationToken stoppingToken)
    {
        var transactionPartyEntities = await context.TransactionParties
            .Include(x => x.Party)
            .Where(x => x.TransactionId == transactionId)
            .ToListAsync(stoppingToken);

        return transactionPartyEntities.ConvertAll(x => x.ToModel());
    }
}
