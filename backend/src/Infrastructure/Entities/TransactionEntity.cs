using System.ComponentModel.DataAnnotations.Schema;
using Core.Enums;
using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record TransactionEntity : DeletableEntity, IAuditable, IModeScoped
{
    public required int ModeId { get; init; }

    public required Guid TransactionId { get; init; }
    public required Guid OrganizationId { get; init; }
    public OrganizationEntity Organization { get; init; } = null!;
    public required string FriendlyName { get; init; }
    public required string? ExternalReference { get; init; }
    public required string Payload { get; init; }

    public ICollection<RuleEvaluationEntity> RuleEvaluations { get; init; } = [];
    public ICollection<CaseEntity> Cases { get; init; } = [];
    public ICollection<TransactionPartyEntity> TransactionParties { get; init; } = [];
    public ICollection<VelocityObservationEntity> VelocityObservations { get; init; } = [];
    public ICollection<AlertEntity> Alerts { get; init; } = [];
    public DecisionEntity? Decision { get; init; }

    [NotMapped]
    public Guid AuditEntityId => TransactionId;

    [NotMapped]
    public Guid? AuditOrganizationId => OrganizationId;

    [NotMapped]
    public AuditableEntityType AuditEntityType => AuditableEntityType.Transaction;

    [NotMapped]
    public IReadOnlyCollection<AuditedProperty> AuditedProperties => AuditedPropertyRegistry.Transaction;
}
