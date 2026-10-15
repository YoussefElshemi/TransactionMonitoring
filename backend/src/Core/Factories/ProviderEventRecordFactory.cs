using Core.Models.Payments;
using Core.Models.Providers;
using Core.ValueObjects;

namespace Core.Factories;

public static class ProviderEventRecordFactory
{
    // Inserted before the event is acted on; the insert claims the event, which is the redelivery defence.
    public static ProviderEventRecord Create(ProviderEvent providerEvent, TimeProvider timeProvider)
    {
        var now = timeProvider.GetUtcNow();

        return new ProviderEventRecord
        {
            ProviderEventRecordId = new ProviderEventRecordId(Guid.NewGuid()),
            ProviderEventReference = providerEvent.EventReference,
            ProviderEventType = providerEvent.EventType,
            Payload = providerEvent.Payload,

            PaymentId = null,
            FailureReason = null,
            IsProcessed = new IsProcessed(false),
            ProcessedAt = null,
            IsDeleted = new IsDeleted(false),
            DeletedAt = null,
            CreatedAt = new CreatedAt(now),
            UpdatedAt = new UpdatedAt(now)
        };
    }
}
