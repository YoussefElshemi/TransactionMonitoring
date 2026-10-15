using Core.Enums;
using Core.Models.Transactions;
using Core.Models.VelocityProfiles;
using Core.ValueObjects;

namespace Core.Interfaces.Repositories;

public interface IVelocityBackfillRepository : IPagedRepository<ListVelocityBackfillRequest, VelocityBackfill>
{
    Task AddAsync(VelocityBackfill velocityBackfill, CancellationToken stoppingToken);
    Task<VelocityBackfill> GetAsync(VelocityBackfillId velocityBackfillId, CancellationToken stoppingToken);
    Task<Exists> ExistsAsync(OrganizationId organizationId, VelocityBackfillId velocityBackfillId, CancellationToken stoppingToken);
    Task UpdateAsync(VelocityBackfillId velocityBackfillId, VelocityBackfillStatus status, TotalCount totalTransactions, ProcessedCount processedCount, CompletedAt? completedAt, CancellationToken stoppingToken);

    Task<TotalCount> CountTransactionsAsync(OrganizationId organizationId, CancellationToken stoppingToken);
    Task<List<Transaction>> ListTransactionChunkAsync(OrganizationId organizationId, ProcessedCount skip, int chunkSize, CancellationToken stoppingToken);
}
