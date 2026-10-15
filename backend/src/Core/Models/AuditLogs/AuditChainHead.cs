using Core.ValueObjects;

namespace Core.Models.AuditLogs;

public record AuditChainHead
{
    public required OrganizationId? OrganizationId { get; init; }
    public required Sequence Sequence { get; init; }
    public required Hash Hash { get; init; }
}
