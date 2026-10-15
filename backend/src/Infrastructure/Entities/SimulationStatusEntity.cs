using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record SimulationStatusEntity : BaseEntity
{
    public required int SimulationStatusId { get; init; }
    public required string Name { get; init; }
    public ICollection<SimulationEntity> Simulations { get; init; } = [];
}
