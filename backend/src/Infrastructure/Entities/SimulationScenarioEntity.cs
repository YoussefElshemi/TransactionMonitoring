using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record SimulationScenarioEntity : BaseEntity
{
    public required int SimulationScenarioId { get; init; }
    public required string Name { get; init; }
    public ICollection<SimulationEntity> Simulations { get; init; } = [];
}
