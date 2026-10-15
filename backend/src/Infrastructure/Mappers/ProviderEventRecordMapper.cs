using Core.Models.Payments;
using Core.ValueObjects;
using Infrastructure.Entities;

namespace Infrastructure.Mappers;

public static class ProviderEventRecordMapper
{
    public static ProviderEventRecord ToModel(this ProviderEventRecordEntity providerEventRecordEntity)
    {
        return new ProviderEventRecord
        {
            ProviderEventRecordId = new ProviderEventRecordId(providerEventRecordEntity.ProviderEventRecordId),
            ProviderEventReference = new ProviderEventReference(providerEventRecordEntity.ProviderEventReference),
            ProviderEventType = new ProviderEventType(providerEventRecordEntity.ProviderEventType),
            Payload = new Payload(providerEventRecordEntity.Payload),
            PaymentId = providerEventRecordEntity.PaymentId.HasValue
                ? new PaymentId(providerEventRecordEntity.PaymentId.Value)
                : null,
            FailureReason = providerEventRecordEntity.FailureReason is null
                ? null
                : new FailureReason(providerEventRecordEntity.FailureReason),
            IsProcessed = new IsProcessed(providerEventRecordEntity.IsProcessed),
            ProcessedAt = providerEventRecordEntity.ProcessedAt.HasValue
                ? new ProcessedAt(providerEventRecordEntity.ProcessedAt.Value)
                : null,
            IsDeleted = new IsDeleted(providerEventRecordEntity.IsDeleted),
            DeletedAt = providerEventRecordEntity.DeletedAt.HasValue
                ? new DeletedAt(providerEventRecordEntity.DeletedAt.Value)
                : null,
            CreatedAt = new CreatedAt(providerEventRecordEntity.CreatedAt),
            UpdatedAt = new UpdatedAt(providerEventRecordEntity.UpdatedAt)
        };
    }

    public static ProviderEventRecordEntity ToEntity(this ProviderEventRecord providerEventRecord)
    {
        return new ProviderEventRecordEntity
        {
            ProviderEventRecordId = providerEventRecord.ProviderEventRecordId,
            ProviderEventReference = providerEventRecord.ProviderEventReference,
            ProviderEventType = providerEventRecord.ProviderEventType,
            Payload = providerEventRecord.Payload,
            PaymentId = providerEventRecord.PaymentId.HasValue ? providerEventRecord.PaymentId.Value : null,
            FailureReason = providerEventRecord.FailureReason?.ToString(),
            IsProcessed = providerEventRecord.IsProcessed,
            ProcessedAt = providerEventRecord.ProcessedAt.HasValue ? providerEventRecord.ProcessedAt.Value : null,
            IsDeleted = providerEventRecord.IsDeleted,
            DeletedAt = providerEventRecord.DeletedAt.HasValue ? providerEventRecord.DeletedAt.Value : null,
            CreatedAt = providerEventRecord.CreatedAt,
            UpdatedAt = providerEventRecord.UpdatedAt
        };
    }
}
