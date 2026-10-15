using Core.Interfaces.Repositories;
using Core.Models.Payments;
using Core.ValueObjects;
using Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Infrastructure.Repositories;

public class ProviderEventRepository(
    TransactionMonitoringDbContext context,
    TimeProvider timeProvider) : IProviderEventRepository
{
    // Insert first: a read-then-write check would let two concurrent redeliveries both through.
    public async Task<IsCreated> TryAddAsync(ProviderEventRecord providerEventRecord, CancellationToken stoppingToken)
    {
        await context.ProviderEventRecords.AddAsync(providerEventRecord.ToEntity(), stoppingToken);

        try
        {
            await context.SaveChangesAsync(stoppingToken);

            return new IsCreated(true);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // Detach, or every later save in this request retries the doomed insert.
            context.ChangeTracker.Clear();

            return new IsCreated(false);
        }
    }

    public async Task<ProviderEventRecord> GetAsync(ProviderEventRecordId providerEventRecordId, CancellationToken stoppingToken)
    {
        var providerEventRecordEntity = await context.ProviderEventRecords
            .FirstAsync(x => x.ProviderEventRecordId == providerEventRecordId, stoppingToken);

        return providerEventRecordEntity.ToModel();
    }

    public async Task ProcessAsync(ProviderEventRecordId providerEventRecordId, PaymentId? paymentId, CancellationToken stoppingToken)
    {
        var providerEventRecordEntity = await context.ProviderEventRecords
            .FirstAsync(x => x.ProviderEventRecordId == providerEventRecordId, stoppingToken);

        providerEventRecordEntity.IsProcessed = true;
        providerEventRecordEntity.ProcessedAt = timeProvider.GetUtcNow();
        providerEventRecordEntity.PaymentId = paymentId.HasValue ? paymentId.Value : null;

        providerEventRecordEntity.FailureReason = null;
        providerEventRecordEntity.UpdatedAt = timeProvider.GetUtcNow();

        context.ProviderEventRecords.Update(providerEventRecordEntity);
    }

    public async Task FailAsync(ProviderEventRecordId providerEventRecordId, FailureReason failureReason, CancellationToken stoppingToken)
    {
        var providerEventRecordEntity = await context.ProviderEventRecords
            .FirstAsync(x => x.ProviderEventRecordId == providerEventRecordId, stoppingToken);

        providerEventRecordEntity.FailureReason = failureReason;
        providerEventRecordEntity.UpdatedAt = timeProvider.GetUtcNow();

        context.ProviderEventRecords.Update(providerEventRecordEntity);
    }
}
