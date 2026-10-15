using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record AuditOperationEntity : BaseEntity
{
    public required int AuditOperationId { get; init; }
    public required string Name { get; init; }
    public ICollection<AuditLogEntity> AuditLogs { get; init; } = [];
}
