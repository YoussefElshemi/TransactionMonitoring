using Core.Models.AuditLogs;
using Core.ValueObjects;
using Infrastructure.Entities;

namespace Infrastructure.Mappers;

public static class AuditAnchorMapper
{
    public static AuditAnchor ToModel(this AuditAnchorEntity auditAnchorEntity)
    {
        return new AuditAnchor
        {
            AuditAnchorId = new AuditAnchorId(auditAnchorEntity.AuditAnchorId),
            OrganizationId = auditAnchorEntity.OrganizationId.HasValue
                ? new OrganizationId(auditAnchorEntity.OrganizationId.Value)
                : null,
            Sequence = new Sequence(auditAnchorEntity.Sequence),
            Hash = new Hash(auditAnchorEntity.Hash),
            Signature = new Signature(auditAnchorEntity.Signature),
            CreatedAt = new CreatedAt(auditAnchorEntity.CreatedAt)
        };
    }

    public static AuditAnchorEntity ToEntity(this AuditAnchor auditAnchor)
    {
        return new AuditAnchorEntity
        {
            AuditAnchorId = auditAnchor.AuditAnchorId,
            OrganizationId = auditAnchor.OrganizationId.HasValue ? (Guid)auditAnchor.OrganizationId.Value : null,
            Sequence = auditAnchor.Sequence,
            Hash = auditAnchor.Hash,
            Signature = auditAnchor.Signature,
            CreatedAt = auditAnchor.CreatedAt
        };
    }
}
