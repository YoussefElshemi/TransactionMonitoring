using Core.Models.Payments;
using Core.ValueObjects;

namespace Core.Interfaces.Repositories;

public interface IProviderEventRepository
{
    // Uniqueness is enforced by the database, not read-then-write, because concurrent redeliveries would both pass a read.
    Task<IsCreated> TryAddAsync(ProviderEventRecord providerEventRecord, CancellationToken stoppingToken);

    Task<ProviderEventRecord> GetAsync(ProviderEventRecordId providerEventRecordId, CancellationToken stoppingToken);
    Task ProcessAsync(ProviderEventRecordId providerEventRecordId, PaymentId? paymentId, CancellationToken stoppingToken);
    Task FailAsync(ProviderEventRecordId providerEventRecordId, FailureReason failureReason, CancellationToken stoppingToken);
}
