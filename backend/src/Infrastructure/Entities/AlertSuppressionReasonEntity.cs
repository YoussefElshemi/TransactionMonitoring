using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record AlertSuppressionReasonEntity : BaseEntity
{
    public required int AlertSuppressionReasonId { get; init; }
    public required string Name { get; init; }
    public ICollection<AlertEntity> Alerts { get; init; } = [];
}
