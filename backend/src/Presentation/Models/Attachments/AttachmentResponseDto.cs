namespace Presentation.Models.Attachments;

public class AttachmentResponseDto
{
    public required Guid AttachmentId { get; init; }
    public required Guid CaseId { get; init; }
    public required Guid OrganizationId { get; init; }
    public required string FileName { get; init; }
    public required string ContentType { get; init; }
    public required long ByteCount { get; init; }
    public required string Checksum { get; init; }
    public required string ScanStatus { get; init; }
    public required DateTimeOffset? ScannedAt { get; init; }
    public required string? ThreatName { get; init; }
    public required Guid UploadedByUserId { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }

    public required string Mode { get; init; }
}
