using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record CaseGroupingSeverityPolicyEntity : BaseEntity
{
    public required int CaseGroupingSeverityPolicyId { get; init; }
    public required string Name { get; init; }
    public ICollection<OrganizationEntity> Organizations { get; init; } = [];
}
