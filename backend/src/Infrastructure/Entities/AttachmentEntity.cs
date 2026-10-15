using System.ComponentModel.DataAnnotations.Schema;
using Core.Enums;
using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record AttachmentEntity : DeletableEntity, IAuditable, IModeScoped
{
    public required int ModeId { get; init; }

    public required Guid AttachmentId { get; init; }
    public required Guid CaseId { get; init; }
    public CaseEntity Case { get; init; } = null!;
    public required Guid OrganizationId { get; init; }
    public OrganizationEntity Organization { get; init; } = null!;
    public required string FileName { get; init; }
    public required string ContentType { get; init; }
    public required long ByteCount { get; init; }
    public required string StorageKey { get; init; }
    public required string Checksum { get; init; }
    public required int ScanStatusId { get; set; }
    public AttachmentScanStatusEntity ScanStatus { get; init; } = null!;
    public required DateTimeOffset? ScannedAt { get; set; }
    public required string? ThreatName { get; set; }
    public required Guid UploadedByUserId { get; init; }
    public ApplicationUserEntity UploadedByUser { get; init; } = null!;

    [NotMapped]
    public Guid AuditEntityId => AttachmentId;

    [NotMapped]
    public Guid? AuditOrganizationId => OrganizationId;

    [NotMapped]
    public AuditableEntityType AuditEntityType => AuditableEntityType.Attachment;

    [NotMapped]
    public IReadOnlyCollection<AuditedProperty> AuditedProperties => AuditedPropertyRegistry.Attachment;
}
