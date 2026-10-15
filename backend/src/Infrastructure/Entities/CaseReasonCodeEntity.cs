using System.ComponentModel.DataAnnotations.Schema;
using Core.Enums;
using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record CaseReasonCodeEntity : DeletableEntity, IAuditable, IModeScoped
{
    public required int ModeId { get; init; }

    public required Guid CaseReasonCodeId { get; init; }
    public required Guid OrganizationId { get; init; }
    public OrganizationEntity Organization { get; init; } = null!;
    public required string Code { get; init; }
    public required string Description { get; set; }
    public required int? DispositionId { get; set; }
    public CaseDispositionEntity? DispositionEntity { get; init; }
    public required bool IsEnabled { get; set; }
    public ICollection<CaseEntity> Cases { get; init; } = [];

    [NotMapped]
    public Guid AuditEntityId => CaseReasonCodeId;

    [NotMapped]
    public Guid? AuditOrganizationId => OrganizationId;

    [NotMapped]
    public AuditableEntityType AuditEntityType => AuditableEntityType.CaseReasonCode;

    [NotMapped]
    public IReadOnlyCollection<AuditedProperty> AuditedProperties => AuditedPropertyRegistry.CaseReasonCode;
}
