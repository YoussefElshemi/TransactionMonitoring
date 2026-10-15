using Core.Enums;
using Core.ValueObjects;

namespace Core.Models.AuditLogs;

public record AuditLog
{
    public required AuditLogId AuditLogId { get; init; }
    public required AuditableEntityType EntityType { get; init; }
    public required Guid EntityId { get; init; }
    public required OrganizationId? OrganizationId { get; init; }
    public required AuditOperation Operation { get; init; }
    public required IReadOnlyList<AuditLogChange> Changes { get; init; }
    public required ApplicationUserId? ChangedByUserId { get; init; }
    public required ApiClientId? ChangedByApiClientId { get; init; }

    // Written by the database trigger, never the caller.
    public required Sequence Sequence { get; init; }

    public required Hash? PreviousHash { get; init; }
    public required Hash Hash { get; init; }
    public required CreatedAt CreatedAt { get; init; }
}
