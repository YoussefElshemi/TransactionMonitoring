using System.ComponentModel.DataAnnotations.Schema;
using Core.Enums;
using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record CaseEntity : DeletableEntity, IAuditable, IModeScoped
{
    public required int ModeId { get; init; }

    public required Guid CaseId { get; init; }
    public required string FriendlyName { get; init; }
    public required int StatusId { get; set; }
    public CaseStatusEntity StatusEntity { get; init; } = null!;
    public required int SeverityId { get; set; }
    public RuleSeverityEntity SeverityEntity { get; init; } = null!;
    public required Guid OrganizationId { get; init; }
    public OrganizationEntity Organization { get; init; } = null!;
    public required Guid? TransactionId { get; init; }
    public TransactionEntity? Transaction { get; init; }
    public required Guid? PartyId { get; init; }
    public PartyEntity? Party { get; init; }
    public required int? DispositionId { get; set; }
    public CaseDispositionEntity? DispositionEntity { get; init; }
    public required Guid? CaseReasonCodeId { get; set; }
    public CaseReasonCodeEntity? ReasonCode { get; init; }
    public required Guid? QueueId { get; set; }
    public QueueEntity? Queue { get; init; }
    public required Guid? AssignedToUserId { get; set; }
    public ApplicationUserEntity? AssignedToUser { get; init; }
    public required DateTimeOffset? AssignedAt { get; set; }
    public required DateTimeOffset DueAt { get; init; }
    public required DateTimeOffset? EscalatedAt { get; set; }
    public required Guid? EscalationRuleId { get; set; }
    public required DateTimeOffset? BreachNotifiedAt { get; set; }
    public EscalationRuleEntity? EscalationRule { get; init; }
    public required DateTimeOffset? ResolvedAt { get; set; }
    public required DateTimeOffset? ReopenedAt { get; set; }
    public ICollection<RuleEvaluationEntity> RuleEvaluations { get; init; } = [];
    public ICollection<CaseRuleEvaluationLinkEntity> CaseRuleEvaluationLinks { get; init; } = [];
    public ICollection<CaseNoteEntity> CaseNotes { get; init; } = [];
    public ICollection<AttachmentEntity> Attachments { get; init; } = [];
    public ICollection<CaseLinkEntity> CaseLinks { get; init; } = [];
    public ICollection<CaseLinkEntity> LinkedFromCases { get; init; } = [];
    public ICollection<AlertEntity> Alerts { get; init; } = [];

    [NotMapped]
    public Guid AuditEntityId => CaseId;

    [NotMapped]
    public Guid? AuditOrganizationId => OrganizationId;

    [NotMapped]
    public AuditableEntityType AuditEntityType => AuditableEntityType.Case;

    [NotMapped]
    public IReadOnlyCollection<AuditedProperty> AuditedProperties => AuditedPropertyRegistry.Case;
}
