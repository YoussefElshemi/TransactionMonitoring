using Core.ValueObjects;

namespace Core.Models.Attachments;

public record CreateAttachmentRequest
{
    public required CaseId CaseId { get; init; }
    public required OrganizationId OrganizationId { get; init; }
    public required FileName FileName { get; init; }
    public required ContentType ContentType { get; init; }

    public required Stream Content { get; init; }
    public required ApplicationUserId UploadedByUserId { get; init; }
}
