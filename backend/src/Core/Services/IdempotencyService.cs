using Core.Configs;
using Core.Enums;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Mappers;
using Core.Models.IdempotencyRecords;
using Core.ValueObjects;
using Microsoft.Extensions.Options;

namespace Core.Services;

public class IdempotencyService(
    IIdempotencyRecordRepository idempotencyRecordRepository,
    IOptions<AppConfig> appConfig,
    TimeProvider timeProvider,
    IModeContext modeContext) : IIdempotencyService
{
    private const int UncompletedStatusCode = 0;

    public async Task<IdempotencyReservation> ReserveAsync(CreateIdempotencyRecordRequest createIdempotencyRecordRequest, CancellationToken stoppingToken)
    {
        var retention = TimeSpan.FromHours(appConfig.Value.IdempotencyConfig.RetentionHours);
        var idempotencyRecord = createIdempotencyRecordRequest.ToModel(timeProvider, modeContext.CurrentOrLive, retention);

        var existing = await idempotencyRecordRepository.TryReserveAsync(idempotencyRecord, stoppingToken);

        if (existing == null)
        {
            return new IdempotencyReservation
            {
                Outcome = IdempotencyOutcome.Reserved,
                Record = null
            };
        }

        if ((string)existing.Resource != (string)createIdempotencyRecordRequest.Resource)
        {
            return new IdempotencyReservation
            {
                Outcome = IdempotencyOutcome.ResourceMismatch,
                Record = null
            };
        }

        if (existing.StatusCode == UncompletedStatusCode)
        {
            return new IdempotencyReservation
            {
                Outcome = IdempotencyOutcome.InProgress,
                Record = null
            };
        }

        return new IdempotencyReservation
        {
            Outcome = IdempotencyOutcome.Replay,
            Record = existing
        };
    }

    public Task CompleteAsync(OrganizationId organizationId, IdempotencyKey idempotencyKey, StatusCode statusCode, ResponseBody responseBody, CancellationToken stoppingToken)
    {
        return idempotencyRecordRepository.CompleteAsync(organizationId, idempotencyKey, statusCode, responseBody, stoppingToken);
    }

    public Task ReleaseAsync(OrganizationId organizationId, IdempotencyKey idempotencyKey, CancellationToken stoppingToken)
    {
        return idempotencyRecordRepository.ReleaseAsync(organizationId, idempotencyKey, stoppingToken);
    }

    public Task<Count> PurgeExpiredAsync(CancellationToken stoppingToken)
    {
        return idempotencyRecordRepository.PurgeExpiredAsync(stoppingToken);
    }
}
