using Core.Enums;
using Core.Models.BaseModels;
using Core.ValueObjects;

namespace Core.Models.Attachments;

public record Attachment : DeletableModel
{
    public required AttachmentId AttachmentId { get; init; }
    public required CaseId CaseId { get; init; }
    public required OrganizationId OrganizationId { get; init; }

    public required FileName FileName { get; init; }
    public required ContentType ContentType { get; init; }
    public required ByteCount ByteCount { get; init; }

    public required StorageKey StorageKey { get; init; }

    public required Checksum Checksum { get; init; }
    public required AttachmentScanStatus ScanStatus { get; init; }
    public required ScannedAt? ScannedAt { get; init; }

    public required ThreatName? ThreatName { get; init; }
    public required ApplicationUserId UploadedByUserId { get; init; }

    public required Mode Mode { get; init; }
}
