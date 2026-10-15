using Core.Enums;
using Core.Models.Common;
using Core.ValueObjects;

namespace Core.Models.AuditLogs;

public record ListAuditLogRequest : PagedRequest
{
    public required OrganizationId OrganizationId { get; init; }
    public required AuditableEntityType EntityType { get; init; }
    public required Guid EntityId { get; init; }
}
