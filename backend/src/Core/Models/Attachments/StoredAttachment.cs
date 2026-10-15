using Core.ValueObjects;

namespace Core.Models.Attachments;

public record StoredAttachment
{
    public required StorageKey StorageKey { get; init; }
    public required ByteCount ByteCount { get; init; }
    public required Checksum Checksum { get; init; }
}
