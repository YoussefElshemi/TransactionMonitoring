using Core.Models.Attachments;
using Presentation.Models.Attachments;

namespace Presentation.Mappers.Attachments;

public static class AttachmentDtoMapper
{
    public static AttachmentResponseDto ToDto(this Attachment attachment)
    {
        return new AttachmentResponseDto
        {
            Mode = attachment.Mode.ToString(),
            AttachmentId = attachment.AttachmentId,
            CaseId = attachment.CaseId,
            OrganizationId = attachment.OrganizationId,
            FileName = attachment.FileName,
            ContentType = attachment.ContentType,
            ByteCount = attachment.ByteCount,
            Checksum = attachment.Checksum,
            ScanStatus = attachment.ScanStatus.ToString(),
            ScannedAt = attachment.ScannedAt.HasValue ? attachment.ScannedAt.Value : null,
            ThreatName = attachment.ThreatName?.ToString(),
            UploadedByUserId = attachment.UploadedByUserId,
            CreatedAt = attachment.CreatedAt,
            UpdatedAt = attachment.UpdatedAt
        };
    }
}
