using Core.Enums;
using Core.Models.AuditLogs;
using Core.ValueObjects;
using Infrastructure.Entities;

namespace Infrastructure.Mappers;

public static class AuditLogMapper
{
    public static AuditLog ToModel(this AuditLogEntity auditLogEntity, IReadOnlyList<AuditLogChange> changes)
    {
        return new AuditLog
        {
            AuditLogId = new AuditLogId(auditLogEntity.AuditLogId),
            EntityType = (AuditableEntityType)auditLogEntity.EntityTypeId,
            EntityId = auditLogEntity.EntityId,
            OrganizationId = auditLogEntity.OrganizationId.HasValue ? new OrganizationId(auditLogEntity.OrganizationId.Value) : null,
            Operation = (AuditOperation)auditLogEntity.OperationId,
            Changes = changes,
            ChangedByUserId = auditLogEntity.ChangedByUserId.HasValue ? new ApplicationUserId(auditLogEntity.ChangedByUserId.Value) : null,
            ChangedByApiClientId = auditLogEntity.ChangedByApiClientId.HasValue ? new ApiClientId(auditLogEntity.ChangedByApiClientId.Value) : null,
            Sequence = new Sequence(auditLogEntity.Sequence),
            PreviousHash = auditLogEntity.PreviousHash is not null ? new Hash(auditLogEntity.PreviousHash) : null,
            Hash = new Hash(auditLogEntity.Hash),
            CreatedAt = new CreatedAt(auditLogEntity.CreatedAt)
        };
    }

    public static AuditLogEntry ToEntry(this AuditLogEntity auditLogEntity)
    {
        return new AuditLogEntry
        {
            AuditLogId = new AuditLogId(auditLogEntity.AuditLogId),
            EntityType = (AuditableEntityType)auditLogEntity.EntityTypeId,
            EntityId = auditLogEntity.EntityId,
            OrganizationId = auditLogEntity.OrganizationId.HasValue ? new OrganizationId(auditLogEntity.OrganizationId.Value) : null,
            Operation = (AuditOperation)auditLogEntity.OperationId,
            Snapshot = new Snapshot(auditLogEntity.Snapshot),
            ChangedByUserId = auditLogEntity.ChangedByUserId.HasValue ? new ApplicationUserId(auditLogEntity.ChangedByUserId.Value) : null,
            ChangedByApiClientId = auditLogEntity.ChangedByApiClientId.HasValue ? new ApiClientId(auditLogEntity.ChangedByApiClientId.Value) : null,
            Sequence = new Sequence(auditLogEntity.Sequence),
            PreviousHash = auditLogEntity.PreviousHash is not null ? new Hash(auditLogEntity.PreviousHash) : null,
            Hash = new Hash(auditLogEntity.Hash),
            CreatedAt = new CreatedAt(auditLogEntity.CreatedAt)
        };
    }
}
