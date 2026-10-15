using Core.Interfaces.Repositories;
using Core.Models.IdempotencyRecords;
using Core.ValueObjects;
using Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class IdempotencyRecordRepository(
    TransactionMonitoringDbContext context,
    TimeProvider timeProvider) : IIdempotencyRecordRepository
{
    public async Task<IdempotencyRecord?> TryReserveAsync(IdempotencyRecord idempotencyRecord, CancellationToken stoppingToken)
    {
        Guid organizationId = idempotencyRecord.OrganizationId;
        Guid idempotencyKey = idempotencyRecord.IdempotencyKey;

        var existing = await context.IdempotencyRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.IdempotencyKey == idempotencyKey, stoppingToken);

        if (existing != null && existing.ExpiresAt > timeProvider.GetUtcNow())
        {
            return existing.ToModel();
        }

        if (existing != null)
        {
            await context.IdempotencyRecords
                .Where(x => x.OrganizationId == organizationId && x.IdempotencyKey == idempotencyKey)
                .ExecuteDeleteAsync(stoppingToken);
        }

        var entry = context.IdempotencyRecords.Add(idempotencyRecord.ToEntity());

        try
        {
            await context.SaveChangesAsync(stoppingToken);

            return null;
        }
        catch (DbUpdateException)
        {
            entry.State = EntityState.Detached;

            var winner = await context.IdempotencyRecords
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.IdempotencyKey == idempotencyKey, stoppingToken);

            return winner?.ToModel();
        }
    }

    public async Task CompleteAsync(OrganizationId organizationId, IdempotencyKey idempotencyKey, StatusCode statusCode, ResponseBody responseBody, CancellationToken stoppingToken)
    {
        Guid organizationIdValue = organizationId;
        Guid idempotencyKeyValue = idempotencyKey;
        int statusCodeValue = statusCode;
        string responseBodyValue = responseBody;
        var updatedAt = timeProvider.GetUtcNow();

        await context.IdempotencyRecords
            .Where(x => x.OrganizationId == organizationIdValue && x.IdempotencyKey == idempotencyKeyValue)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.StatusCode, statusCodeValue)
                .SetProperty(x => x.ResponseBody, responseBodyValue)
                .SetProperty(x => x.UpdatedAt, updatedAt), stoppingToken);
    }

    public async Task ReleaseAsync(OrganizationId organizationId, IdempotencyKey idempotencyKey, CancellationToken stoppingToken)
    {
        Guid organizationIdValue = organizationId;
        Guid idempotencyKeyValue = idempotencyKey;

        await context.IdempotencyRecords
            .Where(x => x.OrganizationId == organizationIdValue && x.IdempotencyKey == idempotencyKeyValue)
            .ExecuteDeleteAsync(stoppingToken);
    }

    public async Task<Count> PurgeExpiredAsync(CancellationToken stoppingToken)
    {
        var now = timeProvider.GetUtcNow();

        var deleted = await context.IdempotencyRecords
            .Where(x => x.ExpiresAt <= now)
            .ExecuteDeleteAsync(stoppingToken);

        return new Count(deleted);
    }
}
