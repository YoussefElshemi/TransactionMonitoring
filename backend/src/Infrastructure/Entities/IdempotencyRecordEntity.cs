using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record IdempotencyRecordEntity : BaseEntity, IModeScoped
{
    public required int ModeId { get; init; }

    public required Guid IdempotencyKey { get; init; }
    public required Guid OrganizationId { get; init; }
    public OrganizationEntity Organization { get; init; } = null!;
    public required string Resource { get; init; }
    public required int StatusCode { get; init; }
    public required string ResponseBody { get; init; }
    public required DateTimeOffset ExpiresAt { get; init; }
}
