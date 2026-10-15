using System.ComponentModel.DataAnnotations.Schema;
using Core.Enums;
using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record SuspiciousActivityReportEntity : BaseEntity, IAuditable, IModeScoped
{
    public required int ModeId { get; init; }

    public required Guid SuspiciousActivityReportId { get; init; }
    public required Guid OrganizationId { get; init; }
    public OrganizationEntity Organization { get; init; } = null!;
    public required Guid CaseId { get; init; }
    public CaseEntity Case { get; init; } = null!;
    public required string FriendlyName { get; init; }
    public required int StatusId { get; set; }
    public SuspiciousActivityReportStatusEntity StatusEntity { get; init; } = null!;
    public required string Narrative { get; set; }
    public required DateTimeOffset DueAt { get; init; }
    public required DateTimeOffset? OverdueNotifiedAt { get; set; }
    public required Guid OpenedByUserId { get; init; }
    public ApplicationUserEntity OpenedByUser { get; init; } = null!;
    public required Guid? FiledByUserId { get; set; }
    public ApplicationUserEntity? FiledByUser { get; init; }
    public required DateTimeOffset? FiledAt { get; set; }
    public required string? FilingReference { get; set; }
    public required DateTimeOffset? AcknowledgedAt { get; set; }
    public required string? AcknowledgementReference { get; set; }
    public required string? NotFiledReason { get; set; }
    public required Guid? DecidedByUserId { get; set; }
    public ApplicationUserEntity? DecidedByUser { get; init; }
    public required DateTimeOffset? DecidedAt { get; set; }

    [NotMapped]
    public Guid AuditEntityId => SuspiciousActivityReportId;

    [NotMapped]
    public Guid? AuditOrganizationId => OrganizationId;

    [NotMapped]
    public AuditableEntityType AuditEntityType => AuditableEntityType.SuspiciousActivityReport;

    [NotMapped]
    public IReadOnlyCollection<AuditedProperty> AuditedProperties => AuditedPropertyRegistry.SuspiciousActivityReport;
}
