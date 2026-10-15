namespace Core.Models.Attachments;

public record AttachmentContent
{
    public required Attachment Attachment { get; init; }
    public required Stream Content { get; init; }
}
