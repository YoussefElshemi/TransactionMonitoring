using Core.Enums;

namespace Infrastructure.Entities;

public interface IAuditable
{
    Guid AuditEntityId { get; }
    Guid? AuditOrganizationId { get; }
    AuditableEntityType AuditEntityType { get; }

    IReadOnlyCollection<AuditedProperty> AuditedProperties { get; }
}
