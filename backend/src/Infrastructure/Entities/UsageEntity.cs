using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record UsageEntity : BaseEntity, IModeScoped
{
    public required int ModeId { get; init; }

    public required Guid UsageId { get; init; }
    public required string Resource { get; init; }

    public required int BillableResourceId { get; init; }
    public BillableResourceEntity BillableResource { get; init; } = null!;
    public required Guid OrganizationId { get; init; }
    public required int Count { get; init; }
    public OrganizationEntity Organization { get; init; } = null!;
}