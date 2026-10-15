using System.ComponentModel.DataAnnotations.Schema;
using Core.Enums;
using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record EscalationRuleEntity : DeletableEntity, IAuditable, IModeScoped
{
    public required int ModeId { get; init; }

    public required Guid EscalationRuleId { get; init; }
    public required Guid OrganizationId { get; init; }
    public OrganizationEntity Organization { get; init; } = null!;
    public required string Name { get; set; }
    public required string? Description { get; set; }
    public required int Priority { get; set; }
    public required bool IsEnabled { get; set; }
    public required int? UnassignedForMinutes { get; set; }
    public required int? HoldExpiryWithinMinutes { get; set; }
    public required int? MinimumSeverityId { get; set; }
    public RuleSeverityEntity? MinimumSeverityEntity { get; init; }

    public required string? Matcher { get; set; }
    public required Guid? TargetQueueId { get; set; }
    public QueueEntity? TargetQueue { get; init; }
    public required int? RaiseSeverityToId { get; set; }
    public RuleSeverityEntity? RaiseSeverityToEntity { get; init; }
    public ICollection<CaseEntity> Cases { get; init; } = [];

    [NotMapped]
    public Guid AuditEntityId => EscalationRuleId;

    [NotMapped]
    public Guid? AuditOrganizationId => OrganizationId;

    [NotMapped]
    public AuditableEntityType AuditEntityType => AuditableEntityType.EscalationRule;

    [NotMapped]
    public IReadOnlyCollection<AuditedProperty> AuditedProperties => AuditedPropertyRegistry.EscalationRule;
}
