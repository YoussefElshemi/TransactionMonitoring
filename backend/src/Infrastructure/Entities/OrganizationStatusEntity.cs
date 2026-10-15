using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record OrganizationStatusEntity : BaseEntity
{
    public required int OrganizationStatusId { get; init; }
    public required string Name { get; init; }
    public ICollection<OrganizationEntity> Organizations { get; init; } = [];
}