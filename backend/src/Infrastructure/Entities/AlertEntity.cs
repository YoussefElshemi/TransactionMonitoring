using System.ComponentModel.DataAnnotations.Schema;
using Core.Enums;
using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record AlertEntity : DeletableEntity, IAuditable, IModeScoped
{
    public required int ModeId { get; init; }

    public required Guid AlertId { get; init; }
    public required string FriendlyName { get; init; }
    public required Guid OrganizationId { get; init; }
    public OrganizationEntity Organization { get; init; } = null!;
    public required int StatusId { get; set; }
    public AlertStatusEntity StatusEntity { get; init; } = null!;
    public required int SeverityId { get; init; }
    public RuleSeverityEntity SeverityEntity { get; init; } = null!;
    public required Guid RuleId { get; init; }
    public RuleEntity Rule { get; init; } = null!;
    public required Guid RuleVersionId { get; init; }
    public RuleVersionEntity RuleVersion { get; init; } = null!;
    public required Guid RuleEvaluationId { get; init; }
    public RuleEvaluationEntity RuleEvaluation { get; init; } = null!;
    public required Guid TransactionId { get; init; }
    public TransactionEntity Transaction { get; init; } = null!;
    public required Guid? PartyId { get; init; }
    public PartyEntity? Party { get; init; }
    public required int? PartyRoleId { get; init; }
    public PartyRoleEntity? PartyRoleEntity { get; init; }
    public required Guid? CaseId { get; set; }
    public CaseEntity? Case { get; init; }
    public required int? SuppressionReasonId { get; set; }
    public AlertSuppressionReasonEntity? SuppressionReasonEntity { get; init; }
    public required Guid? SuppressedByAlertId { get; set; }
    public required DateTimeOffset RaisedAt { get; init; }
    public required DateTimeOffset? ClosedAt { get; set; }

    [NotMapped]
    public Guid AuditEntityId => AlertId;

    [NotMapped]
    public Guid? AuditOrganizationId => OrganizationId;

    [NotMapped]
    public AuditableEntityType AuditEntityType => AuditableEntityType.Alert;

    [NotMapped]
    public IReadOnlyCollection<AuditedProperty> AuditedProperties => AuditedPropertyRegistry.Alert;
}
