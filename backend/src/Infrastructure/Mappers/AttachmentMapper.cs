using Core.Enums;
using Core.Models.Attachments;
using Core.ValueObjects;
using Infrastructure.Entities;

namespace Infrastructure.Mappers;

public static class AttachmentMapper
{
    public static Attachment ToModel(this AttachmentEntity attachmentEntity)
    {
        return new Attachment
        {
            Mode = (Mode)attachmentEntity.ModeId,
            AttachmentId = new AttachmentId(attachmentEntity.AttachmentId),
            CaseId = new CaseId(attachmentEntity.CaseId),
            OrganizationId = new OrganizationId(attachmentEntity.OrganizationId),
            FileName = new FileName(attachmentEntity.FileName),
            ContentType = new ContentType(attachmentEntity.ContentType),
            ByteCount = new ByteCount(attachmentEntity.ByteCount),
            StorageKey = new StorageKey(attachmentEntity.StorageKey),
            Checksum = new Checksum(attachmentEntity.Checksum),
            ScanStatus = (AttachmentScanStatus)attachmentEntity.ScanStatusId,
            ScannedAt = attachmentEntity.ScannedAt.HasValue ? new ScannedAt(attachmentEntity.ScannedAt.Value) : null,
            ThreatName = attachmentEntity.ThreatName is null ? null : new ThreatName(attachmentEntity.ThreatName),
            UploadedByUserId = new ApplicationUserId(attachmentEntity.UploadedByUserId),
            CreatedAt = new CreatedAt(attachmentEntity.CreatedAt),
            UpdatedAt = new UpdatedAt(attachmentEntity.UpdatedAt),
            IsDeleted = new IsDeleted(attachmentEntity.IsDeleted),
            DeletedAt = attachmentEntity.DeletedAt.HasValue ? new DeletedAt(attachmentEntity.DeletedAt.Value) : null
        };
    }

    public static AttachmentEntity ToEntity(this Attachment attachment)
    {
        return new AttachmentEntity
        {
            ModeId = (int)attachment.Mode,
            AttachmentId = attachment.AttachmentId,
            CaseId = attachment.CaseId,
            OrganizationId = attachment.OrganizationId,
            FileName = attachment.FileName,
            ContentType = attachment.ContentType,
            ByteCount = attachment.ByteCount,
            StorageKey = attachment.StorageKey,
            Checksum = attachment.Checksum,
            ScanStatusId = (int)attachment.ScanStatus,
            ScannedAt = attachment.ScannedAt.HasValue ? attachment.ScannedAt.Value : null,
            ThreatName = attachment.ThreatName?.ToString(),
            UploadedByUserId = attachment.UploadedByUserId,
            CreatedAt = attachment.CreatedAt,
            UpdatedAt = attachment.UpdatedAt,
            IsDeleted = attachment.IsDeleted,
            DeletedAt = attachment.DeletedAt
        };
    }
}
