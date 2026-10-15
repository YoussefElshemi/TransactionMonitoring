using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record DiscountTypeEntity : BaseEntity
{
    public required int DiscountTypeId { get; init; }
    public required string Name { get; init; }
}
