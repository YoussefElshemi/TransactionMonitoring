using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record OrganizationDataKeyEntity : BaseEntity
{
    public required Guid OrganizationId { get; init; }
    public OrganizationEntity Organization { get; init; } = null!;
    public required string WrappedDataKey { get; init; }
}
