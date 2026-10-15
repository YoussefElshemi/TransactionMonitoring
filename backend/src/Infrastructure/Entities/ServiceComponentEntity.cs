using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record ServiceComponentEntity : BaseEntity
{
    public required int ServiceComponentId { get; init; }
    public required string Name { get; init; }
}
