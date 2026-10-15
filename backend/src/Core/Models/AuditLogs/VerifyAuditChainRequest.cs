using Core.Models.Common;
using Core.ValueObjects;

namespace Core.Models.AuditLogs;

public record VerifyAuditChainRequest : DatedRequest
{
    public required OrganizationId OrganizationId { get; init; }
}
