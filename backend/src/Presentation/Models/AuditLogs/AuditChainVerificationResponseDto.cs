namespace Presentation.Models.AuditLogs;

public record AuditChainVerificationResponseDto
{
    public required Guid OrganizationId { get; init; }
    public required int EntryCount { get; init; }
    public required int AnchorCount { get; init; }
    public required bool IsIntact { get; init; }
    public required Guid? FirstBrokenAuditLogId { get; init; }
    public required long? FirstBrokenSequence { get; init; }
    public required Guid? FirstBrokenAuditAnchorId { get; init; }
    public required DateTimeOffset VerifiedAt { get; init; }
}
