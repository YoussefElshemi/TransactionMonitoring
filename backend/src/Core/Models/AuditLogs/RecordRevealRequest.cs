using Core.Enums;
using Core.ValueObjects;

namespace Core.Models.AuditLogs;

public record RecordRevealRequest
{
    public required AuditableEntityType EntityType { get; init; }
    public required Guid EntityId { get; init; }
    public required OrganizationId OrganizationId { get; init; }

    public required RevealReason Reason { get; init; }
}
