using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record PartyRoleEntity : BaseEntity
{
    public required int PartyRoleId { get; init; }
    public required string Name { get; init; }
    public ICollection<TransactionPartyEntity> TransactionParties { get; init; } = [];
    public ICollection<VelocityObservationEntity> VelocityObservations { get; init; } = [];
    public ICollection<RuleEntity> Rules { get; init; } = [];
    public ICollection<AlertEntity> Alerts { get; init; } = [];
}
