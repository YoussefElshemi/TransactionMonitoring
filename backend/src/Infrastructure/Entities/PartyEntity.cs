using System.ComponentModel.DataAnnotations.Schema;
using Core.Enums;
using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record PartyEntity : BaseEntity, IAuditable, IModeScoped
{
    public required int ModeId { get; init; }

    public required Guid PartyId { get; init; }
    public required Guid OrganizationId { get; init; }
    public OrganizationEntity Organization { get; init; } = null!;
    public required string Identifier { get; init; }
    public required string? DisplayName { get; set; }

    public required int? RiskScore { get; set; }
    public required string? RiskNote { get; set; }
    public required Guid? RiskSetByUserId { get; set; }
    public ApplicationUserEntity? RiskSetByUser { get; init; }
    public required DateTimeOffset? RiskSetAt { get; set; }

    public required DateTimeOffset FirstSeenAt { get; init; }
    public required DateTimeOffset LastSeenAt { get; set; }
    public required string Attributes { get; set; }

    public ICollection<TransactionPartyEntity> TransactionParties { get; init; } = [];
    public ICollection<VelocityObservationEntity> VelocityObservations { get; init; } = [];
    public ICollection<AlertEntity> Alerts { get; init; } = [];
    public ICollection<CaseEntity> Cases { get; init; } = [];

    [NotMapped]
    public Guid AuditEntityId => PartyId;

    [NotMapped]
    public Guid? AuditOrganizationId => OrganizationId;

    [NotMapped]
    public AuditableEntityType AuditEntityType => AuditableEntityType.Party;

    [NotMapped]
    public IReadOnlyCollection<AuditedProperty> AuditedProperties => AuditedPropertyRegistry.Party;
}
