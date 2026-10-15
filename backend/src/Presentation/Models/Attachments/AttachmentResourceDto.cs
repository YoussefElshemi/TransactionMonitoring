using System.Text.Json.Serialization;
using Presentation.Models.Users;

namespace Presentation.Models.Attachments;

public record AttachmentResourceDto
{
    public required AttachmentResponseDto Attachment { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public UserResponseDto? UploadedByUser { get; init; }
}
