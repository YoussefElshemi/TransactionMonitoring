using Core.Enums;
using Core.ValueObjects;

namespace Core.Models.Attachments;

public record ScanResult
{
    public required AttachmentScanStatus ScanStatus { get; init; }
    public required ThreatName? ThreatName { get; init; }
}
