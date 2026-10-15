using Core.Enums;
using Core.Models.Common;
using Core.ValueObjects;

namespace Core.Models.Attachments;

public record ListAttachmentRequest : PagedRequest
{
    public required CaseId CaseId { get; init; }
    public required OrganizationId OrganizationId { get; init; }
    public required FileName? FileName { get; init; }

    public required AttachmentScanStatus? ScanStatus { get; init; }
}
