using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record VelocityObservationEntity : BaseEntity, IModeScoped
{
    public required int ModeId { get; init; }

    public required Guid VelocityObservationId { get; init; }
    public required Guid OrganizationId { get; init; }
    public OrganizationEntity Organization { get; init; } = null!;
    public required Guid PartyId { get; init; }
    public PartyEntity Party { get; init; } = null!;
    public required int PartyRoleId { get; init; }
    public PartyRoleEntity PartyRoleEntity { get; init; } = null!;
    public required string Metric { get; init; }
    public required Guid TransactionId { get; init; }
    public TransactionEntity Transaction { get; init; } = null!;
    public required DateTimeOffset ObservedAt { get; init; }
    public required string Text { get; init; }
    public required decimal? Number { get; init; }
}
