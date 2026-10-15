using System.ComponentModel.DataAnnotations.Schema;
using Core.Enums;
using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record DecisionEntity : BaseEntity, IAuditable, IModeScoped
{
    public required int ModeId { get; init; }

    public required Guid DecisionId { get; init; }
    public required Guid OrganizationId { get; init; }
    public OrganizationEntity Organization { get; init; } = null!;
    public required Guid TransactionId { get; init; }
    public TransactionEntity Transaction { get; init; } = null!;
    public required int StatusId { get; set; }
    public DecisionStatusEntity StatusEntity { get; init; } = null!;
    public required int ReasonCodeId { get; set; }
    public DecisionReasonCodeEntity ReasonCodeEntity { get; init; } = null!;
    public required Guid? CaseId { get; init; }
    public CaseEntity? Case { get; init; }
    public required Guid? ResolvedByUserId { get; set; }
    public ApplicationUserEntity? ResolvedByUser { get; init; }
    public required DateTimeOffset? HeldAt { get; init; }
    public required DateTimeOffset? ExpiresAt { get; init; }
    public required int? HoldExpiryVerdictId { get; init; }
    public HoldExpiryVerdictEntity? HoldExpiryVerdictEntity { get; init; }
    public required DateTimeOffset? ResolvedAt { get; set; }
    public ICollection<RuleEvaluationEntity> RuleEvaluations { get; init; } = [];
    public ICollection<DecisionRuleEvaluationLinkEntity> DecisionRuleEvaluationLinks { get; init; } = [];

    [NotMapped]
    public Guid AuditEntityId => DecisionId;

    [NotMapped]
    public Guid? AuditOrganizationId => OrganizationId;

    [NotMapped]
    public AuditableEntityType AuditEntityType => AuditableEntityType.Decision;

    [NotMapped]
    public IReadOnlyCollection<AuditedProperty> AuditedProperties => AuditedPropertyRegistry.Decision;

    // Identity equality: the Transaction/Decision pair makes record structural equality recurse forever.
    public virtual bool Equals(DecisionEntity? other)
    {
        return other is not null && DecisionId == other.DecisionId;
    }

    public override int GetHashCode()
    {
        return DecisionId.GetHashCode();
    }
}
