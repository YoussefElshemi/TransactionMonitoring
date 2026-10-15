using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record AuditableEntityTypeEntity : BaseEntity
{
    public required int AuditableEntityTypeId { get; init; }
    public required string Name { get; init; }
    public ICollection<AuditLogEntity> AuditLogs { get; init; } = [];
}
