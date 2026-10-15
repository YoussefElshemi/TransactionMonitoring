using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record BillableResourceEntity : BaseEntity
{
    public required int BillableResourceId { get; init; }
    public required string Name { get; init; }
}
