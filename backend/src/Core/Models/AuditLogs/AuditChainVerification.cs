using Core.ValueObjects;

namespace Core.Models.AuditLogs;

public record AuditChainVerification
{
    public required OrganizationId OrganizationId { get; init; }
    public required TotalCount EntryCount { get; init; }
    public required TotalCount AnchorCount { get; init; }
    public required IsIntact IsIntact { get; init; }

    public required AuditLogId? FirstBrokenAuditLogId { get; init; }
    public required Sequence? FirstBrokenSequence { get; init; }

    public required AuditAnchorId? FirstBrokenAuditAnchorId { get; init; }
    public required VerifiedAt VerifiedAt { get; init; }
}
