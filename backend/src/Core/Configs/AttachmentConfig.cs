namespace Core.Configs;

public record AttachmentConfig
{
    public string RootPath { get; init; } = "attachments";

    public long MaxByteCount { get; init; } = 25 * 1024 * 1024;

    public string[] AllowedContentTypes { get; init; } =
    [
        "application/pdf",
        "image/png",
        "image/jpeg",
        "image/gif",
        "text/plain",
        "text/csv",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "message/rfc822"
    ];
}
