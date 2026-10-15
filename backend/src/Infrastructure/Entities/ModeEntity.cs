using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record ModeEntity : BaseEntity
{
    public required int ModeId { get; init; }
    public required string Name { get; init; }
}
