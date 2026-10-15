using System.ComponentModel.DataAnnotations.Schema;
using Core.Enums;
using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record PartyMappingEntity : DeletableEntity, IAuditable, IModeScoped
{
    public required int ModeId { get; init; }

    public required Guid PartyMappingId { get; init; }
    public required Guid OrganizationId { get; init; }
    public OrganizationEntity Organization { get; init; } = null!;
    public required string Mappings { get; set; }
    public required Guid CurrentVersionId { get; set; }
    public required int CurrentVersionNumber { get; set; }
    public required Guid CreatedByUserId { get; init; }
    public ApplicationUserEntity CreatedByUser { get; init; } = null!;
    public ICollection<PartyMappingVersionEntity> Versions { get; init; } = [];

    [NotMapped]
    public Guid AuditEntityId => PartyMappingId;

    [NotMapped]
    public Guid? AuditOrganizationId => OrganizationId;

    [NotMapped]
    public AuditableEntityType AuditEntityType => AuditableEntityType.PartyMapping;

    [NotMapped]
    public IReadOnlyCollection<AuditedProperty> AuditedProperties => AuditedPropertyRegistry.PartyMapping;
}
