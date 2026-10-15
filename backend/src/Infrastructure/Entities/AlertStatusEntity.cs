using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record AlertStatusEntity : BaseEntity
{
    public required int AlertStatusId { get; init; }
    public required string Name { get; init; }
    public ICollection<AlertEntity> Alerts { get; init; } = [];
}
