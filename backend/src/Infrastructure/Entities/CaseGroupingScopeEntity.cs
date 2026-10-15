using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record CaseGroupingScopeEntity : BaseEntity
{
    public required int CaseGroupingScopeId { get; init; }
    public required string Name { get; init; }
    public ICollection<OrganizationEntity> Organizations { get; init; } = [];
}
