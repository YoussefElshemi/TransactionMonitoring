using System.ComponentModel.DataAnnotations.Schema;
using Core.Enums;
using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record RuleSetEntity : DeletableEntity, IAuditable, IModeScoped
{
    public required int ModeId { get; init; }

    public required Guid RuleSetId { get; init; }
    public required string FriendlyName { get; set; }
    public required Guid OrganizationId { get; init; }
    public OrganizationEntity Organization { get; init; } = null!;
    public required Guid? SubOrganizationId { get; set; }
    public SubOrganizationEntity? SubOrganization { get; init; }
    public required bool IsEnabled { get; set; }
    public required int? ReviewThreshold { get; set; }
    public required Guid CreatedByUserId { get; init; }
    public ApplicationUserEntity CreatedByUser { get; init; } = null!;
    public ICollection<RuleEntity> Rules { get; init; } = [];

    [NotMapped]
    public Guid AuditEntityId => RuleSetId;

    [NotMapped]
    public Guid? AuditOrganizationId => OrganizationId;

    [NotMapped]
    public AuditableEntityType AuditEntityType => AuditableEntityType.RuleSet;

    [NotMapped]
    public IReadOnlyCollection<AuditedProperty> AuditedProperties => AuditedPropertyRegistry.RuleSet;
}