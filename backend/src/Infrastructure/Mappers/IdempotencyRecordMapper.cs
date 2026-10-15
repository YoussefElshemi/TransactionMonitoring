using Core.Enums;
using Core.Models.IdempotencyRecords;
using Core.ValueObjects;
using Infrastructure.Entities;

namespace Infrastructure.Mappers;

public static class IdempotencyRecordMapper
{
    public static IdempotencyRecord ToModel(this IdempotencyRecordEntity idempotencyRecordEntity)
    {
        return new IdempotencyRecord
        {
            Mode = (Mode)idempotencyRecordEntity.ModeId,
            IdempotencyKey = new IdempotencyKey(idempotencyRecordEntity.IdempotencyKey),
            OrganizationId = new OrganizationId(idempotencyRecordEntity.OrganizationId),
            Resource = new Resource(idempotencyRecordEntity.Resource),
            StatusCode = new StatusCode(idempotencyRecordEntity.StatusCode),
            ResponseBody = new ResponseBody(idempotencyRecordEntity.ResponseBody),
            ExpiresAt = new ExpiresAt(idempotencyRecordEntity.ExpiresAt),
            CreatedAt = new CreatedAt(idempotencyRecordEntity.CreatedAt),
            UpdatedAt = new UpdatedAt(idempotencyRecordEntity.UpdatedAt)
        };
    }

    public static IdempotencyRecordEntity ToEntity(this IdempotencyRecord idempotencyRecord)
    {
        return new IdempotencyRecordEntity
        {
            ModeId = (int)idempotencyRecord.Mode,
            IdempotencyKey = idempotencyRecord.IdempotencyKey,
            OrganizationId = idempotencyRecord.OrganizationId,
            Resource = idempotencyRecord.Resource,
            StatusCode = idempotencyRecord.StatusCode,
            ResponseBody = idempotencyRecord.ResponseBody,
            ExpiresAt = idempotencyRecord.ExpiresAt,
            CreatedAt = idempotencyRecord.CreatedAt,
            UpdatedAt = idempotencyRecord.UpdatedAt
        };
    }
}
