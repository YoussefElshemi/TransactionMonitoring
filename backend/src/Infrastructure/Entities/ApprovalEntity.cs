using System.ComponentModel.DataAnnotations.Schema;
using Core.Enums;
using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record ApprovalEntity : BaseEntity, IAuditable, IModeScoped
{
    public required int ModeId { get; init; }

    public required Guid ApprovalId { get; init; }
    public required Guid OrganizationId { get; init; }
    public OrganizationEntity Organization { get; init; } = null!;
    public required int SubjectId { get; init; }
    public ApprovalSubjectEntity SubjectEntity { get; init; } = null!;

    public required Guid? RuleDeploymentId { get; init; }
    public RuleDeploymentEntity? RuleDeployment { get; init; }
    public required Guid? CaseId { get; init; }
    public CaseEntity? Case { get; init; }
    public required Guid? SuspiciousActivityReportId { get; init; }
    public SuspiciousActivityReportEntity? SuspiciousActivityReport { get; init; }

    public required int? ProposedStatusId { get; init; }
    public CaseStatusEntity? ProposedStatusEntity { get; init; }
    public required int? ProposedDispositionId { get; init; }
    public CaseDispositionEntity? ProposedDispositionEntity { get; init; }
    public required Guid? ProposedCaseReasonCodeId { get; init; }
    public CaseReasonCodeEntity? ProposedCaseReasonCode { get; init; }
    public required string? ProposedNote { get; init; }

    public required int StatusId { get; set; }
    public ApprovalStatusEntity StatusEntity { get; init; } = null!;
    public required string? Reason { get; init; }
    public required Guid RequestedByUserId { get; init; }
    public ApplicationUserEntity RequestedByUser { get; init; } = null!;
    public required Guid? ReviewedByUserId { get; set; }
    public ApplicationUserEntity? ReviewedByUser { get; init; }
    public required DateTimeOffset? ReviewedAt { get; set; }
    public required string? ReviewNote { get; set; }

    [NotMapped]
    public Guid AuditEntityId => ApprovalId;

    [NotMapped]
    public Guid? AuditOrganizationId => OrganizationId;

    [NotMapped]
    public AuditableEntityType AuditEntityType => AuditableEntityType.Approval;

    [NotMapped]
    public IReadOnlyCollection<AuditedProperty> AuditedProperties => AuditedPropertyRegistry.Approval;
}
