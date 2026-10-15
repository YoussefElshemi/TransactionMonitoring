using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record DunningStageEntity : BaseEntity
{
    public required int DunningStageId { get; init; }
    public required string Name { get; init; }
}
