using Core.ValueObjects;

namespace Core.Models.AuditLogs;

public record AuditAnchor
{
    public required AuditAnchorId AuditAnchorId { get; init; }

    public required OrganizationId? OrganizationId { get; init; }

    public required Sequence Sequence { get; init; }
    public required Hash Hash { get; init; }
    public required Signature Signature { get; init; }
    public required CreatedAt CreatedAt { get; init; }
}
