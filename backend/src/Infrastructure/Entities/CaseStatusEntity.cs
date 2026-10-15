using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record CaseStatusEntity : BaseEntity
{
    public required int CaseStatusId { get; init; }
    public required string Name { get; init; }
    public ICollection<CaseEntity> Cases { get; init; } = [];
}