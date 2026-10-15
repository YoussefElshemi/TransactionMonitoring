using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record TransactionPartyEntity : BaseEntity
{
    public required Guid TransactionId { get; init; }
    public TransactionEntity Transaction { get; init; } = null!;
    public required Guid PartyId { get; init; }
    public PartyEntity Party { get; init; } = null!;
    public required int PartyRoleId { get; init; }
    public PartyRoleEntity PartyRoleEntity { get; init; } = null!;
}
