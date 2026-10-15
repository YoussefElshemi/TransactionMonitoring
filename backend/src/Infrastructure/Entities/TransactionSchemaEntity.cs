using System.ComponentModel.DataAnnotations.Schema;
using Core.Enums;
using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record TransactionSchemaEntity : DeletableEntity, IAuditable, IModeScoped
{
    public required int ModeId { get; init; }

    public required Guid TransactionSchemaId { get; init; }
    public required Guid OrganizationId { get; init; }
    public OrganizationEntity Organization { get; init; } = null!;
    public required string Fields { get; set; }
    public required Guid CurrentVersionId { get; set; }
    public required int CurrentVersionNumber { get; set; }
    public required Guid CreatedByUserId { get; init; }
    public ApplicationUserEntity CreatedByUser { get; init; } = null!;
    public ICollection<TransactionSchemaVersionEntity> Versions { get; init; } = [];

    [NotMapped]
    public Guid AuditEntityId => TransactionSchemaId;

    [NotMapped]
    public Guid? AuditOrganizationId => OrganizationId;

    [NotMapped]
    public AuditableEntityType AuditEntityType => AuditableEntityType.TransactionSchema;

    [NotMapped]
    public IReadOnlyCollection<AuditedProperty> AuditedProperties => AuditedPropertyRegistry.TransactionSchema;
}
