using Core.Models.IdempotencyRecords;
using Core.ValueObjects;

namespace Core.Interfaces.Repositories;

public interface IIdempotencyRecordRepository
{
    Task<IdempotencyRecord?> TryReserveAsync(IdempotencyRecord idempotencyRecord, CancellationToken stoppingToken);

    Task CompleteAsync(OrganizationId organizationId, IdempotencyKey idempotencyKey, StatusCode statusCode, ResponseBody responseBody, CancellationToken stoppingToken);

    Task ReleaseAsync(OrganizationId organizationId, IdempotencyKey idempotencyKey, CancellationToken stoppingToken);

    Task<Count> PurgeExpiredAsync(CancellationToken stoppingToken);
}
