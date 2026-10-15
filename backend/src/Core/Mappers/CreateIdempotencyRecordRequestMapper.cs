using Core.Enums;
using Core.Models.IdempotencyRecords;
using Core.ValueObjects;

namespace Core.Mappers;

public static class CreateIdempotencyRecordRequestMapper
{
    public static IdempotencyRecord ToModel(this CreateIdempotencyRecordRequest createIdempotencyRecordRequest, TimeProvider timeProvider, Mode mode, TimeSpan retention)
    {
        return new IdempotencyRecord
        {
            Mode = mode,
            IdempotencyKey = createIdempotencyRecordRequest.IdempotencyKey,
            OrganizationId = createIdempotencyRecordRequest.OrganizationId,
            Resource = createIdempotencyRecordRequest.Resource,
            StatusCode = new StatusCode(0),
            ResponseBody = new ResponseBody(string.Empty),
            ExpiresAt = new ExpiresAt(timeProvider.GetUtcNow().Add(retention)),
            CreatedAt = new CreatedAt(timeProvider.GetUtcNow()),
            UpdatedAt = new UpdatedAt(timeProvider.GetUtcNow())
        };
    }
}
