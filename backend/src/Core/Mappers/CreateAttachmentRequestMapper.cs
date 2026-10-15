using Core.Enums;
using Core.Models.Attachments;
using Core.ValueObjects;

namespace Core.Mappers;

public static class CreateAttachmentRequestMapper
{
    public static Attachment ToModel(
        this CreateAttachmentRequest createAttachmentRequest,
        AttachmentId attachmentId,
        StoredAttachment storedAttachment,
        TimeProvider timeProvider, Mode mode)
    {
        return new Attachment
        {
            Mode = mode,
            AttachmentId = attachmentId,
            CaseId = createAttachmentRequest.CaseId,
            OrganizationId = createAttachmentRequest.OrganizationId,
            FileName = createAttachmentRequest.FileName,
            ContentType = createAttachmentRequest.ContentType,
            ByteCount = storedAttachment.ByteCount,
            StorageKey = storedAttachment.StorageKey,
            Checksum = storedAttachment.Checksum,
            ScanStatus = AttachmentScanStatus.Pending,
            ScannedAt = null,
            ThreatName = null,
            UploadedByUserId = createAttachmentRequest.UploadedByUserId,
            CreatedAt = new CreatedAt(timeProvider.GetUtcNow()),
            UpdatedAt = new UpdatedAt(timeProvider.GetUtcNow()),
            IsDeleted = new IsDeleted(false),
            DeletedAt = null
        };
    }
}
