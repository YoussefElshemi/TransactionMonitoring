using Core.Models.AuditLogs;
using Presentation.Models.AuditLogs;

namespace Presentation.Mappers.AuditLogs;

public static class AuditLogDtoMapper
{
    public static AuditLogResponseDto ToDto(this AuditLog auditLog)
    {
        return new AuditLogResponseDto
        {
            AuditLogId = auditLog.AuditLogId,
            EntityType = auditLog.EntityType.ToString(),
            EntityId = auditLog.EntityId,
            OrganizationId = auditLog.OrganizationId,
            Operation = auditLog.Operation.ToString(),
            Changes = auditLog.Changes.Select(x => x.ToDto()).ToArray(),
            ChangedByUserId = auditLog.ChangedByUserId,
            ChangedByApiClientId = auditLog.ChangedByApiClientId,
            Sequence = auditLog.Sequence,
            PreviousHash = auditLog.PreviousHash?.ToString(),
            Hash = auditLog.Hash,
            CreatedAt = auditLog.CreatedAt
        };
    }
}
