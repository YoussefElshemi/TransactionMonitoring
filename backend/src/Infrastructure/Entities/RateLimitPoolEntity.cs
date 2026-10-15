using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record RateLimitPoolEntity : BaseEntity
{
    public required int RateLimitPoolId { get; init; }
    public required string Name { get; init; }
    public ICollection<RateLimitOverrideEntity> RateLimitOverrides { get; init; } = [];
}
