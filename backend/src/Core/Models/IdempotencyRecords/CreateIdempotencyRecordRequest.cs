using Core.ValueObjects;

namespace Core.Models.IdempotencyRecords;

public record CreateIdempotencyRecordRequest
{
    public required IdempotencyKey IdempotencyKey { get; init; }
    public required OrganizationId OrganizationId { get; init; }
    public required Resource Resource { get; init; }
}
