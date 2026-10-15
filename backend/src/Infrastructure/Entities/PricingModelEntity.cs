using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record PricingModelEntity : BaseEntity
{
    public required int PricingModelId { get; init; }
    public required string Name { get; init; }
}
