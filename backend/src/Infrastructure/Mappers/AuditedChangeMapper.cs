using Core.Models.AuditLogs;
using Infrastructure.Entities;

namespace Infrastructure.Mappers;

public static class AuditedChangeMapper
{
    public static List<AuditLogChange> Apply(
        IReadOnlyList<AuditLogChange> changes,
        IReadOnlyCollection<AuditedProperty> auditedProperties)
    {
        var propertiesByName = auditedProperties.ToDictionary(property => property.Name);

        return changes.Select(change => Map(change, propertiesByName)).ToList();
    }

    private static AuditLogChange Map(AuditLogChange change, IReadOnlyDictionary<string, AuditedProperty> propertiesByName)
    {
        if (!propertiesByName.TryGetValue(change.Field, out var property))
        {
            return change;
        }

        return change with
        {
            Field = property.DisplayName ?? change.Field,
            OldValue = property.ValueSelector is null ? change.OldValue : property.ValueSelector(change.OldValue),
            NewValue = property.ValueSelector is null ? change.NewValue : property.ValueSelector(change.NewValue)
        };
    }
}
