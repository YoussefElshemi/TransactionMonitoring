using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record AccountEntryReasonEntity : BaseEntity
{
    public required int AccountEntryReasonId { get; init; }
    public required string Name { get; init; }
}
