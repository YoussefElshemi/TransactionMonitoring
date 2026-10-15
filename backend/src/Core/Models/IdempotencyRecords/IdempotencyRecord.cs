using Core.Enums;
using Core.Models.BaseModels;
using Core.ValueObjects;

namespace Core.Models.IdempotencyRecords;

public record IdempotencyRecord : BaseModel
{
    public required IdempotencyKey IdempotencyKey { get; init; }
    public required OrganizationId OrganizationId { get; init; }
    public required Resource Resource { get; init; }
    public required StatusCode StatusCode { get; init; }
    public required ResponseBody ResponseBody { get; init; }
    public required ExpiresAt ExpiresAt { get; init; }

    public required Mode Mode { get; init; }
}
