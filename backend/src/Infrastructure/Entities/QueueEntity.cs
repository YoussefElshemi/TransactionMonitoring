using System.ComponentModel.DataAnnotations.Schema;
using Core.Enums;
using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record QueueEntity : DeletableEntity, IAuditable, IModeScoped
{
    public required int ModeId { get; init; }

    public required Guid QueueId { get; init; }
    public required Guid OrganizationId { get; init; }
    public OrganizationEntity Organization { get; init; } = null!;
    public required Guid? SubOrganizationId { get; set; }
    public SubOrganizationEntity? SubOrganization { get; init; }
    public required string Name { get; set; }
    public required string? Description { get; set; }
    public required int Priority { get; set; }
    public required int RoutingPolicyId { get; set; }
    public QueueRoutingPolicyEntity RoutingPolicyEntity { get; init; } = null!;
    public required int? MinimumSeverityId { get; set; }
    public RuleSeverityEntity? MinimumSeverityEntity { get; init; }

    public required string? Matcher { get; set; }
    public required bool IsEnabled { get; set; }
    public ICollection<QueueRuleEntity> QueueRules { get; init; } = [];
    public ICollection<QueueMemberEntity> Members { get; init; } = [];
    public ICollection<CaseEntity> Cases { get; init; } = [];
    public ICollection<EscalationRuleEntity> EscalationRules { get; init; } = [];

    [NotMapped]
    public Guid AuditEntityId => QueueId;

    [NotMapped]
    public Guid? AuditOrganizationId => OrganizationId;

    [NotMapped]
    public AuditableEntityType AuditEntityType => AuditableEntityType.Queue;

    [NotMapped]
    public IReadOnlyCollection<AuditedProperty> AuditedProperties => AuditedPropertyRegistry.Queue;
}
