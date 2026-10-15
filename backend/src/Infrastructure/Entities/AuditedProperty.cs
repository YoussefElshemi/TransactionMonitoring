using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Infrastructure.Entities;

public sealed record AuditedProperty
{
    public required string Name { get; init; }
    public string? DisplayName { get; private init; }
    public Func<JsonNode?, JsonNode?>? ValueSelector { get; private init; }
    public Func<EntityEntry, object?>? SnapshotSelector { get; private init; }

    public static implicit operator AuditedProperty(string name)
    {
        return new AuditedProperty
        {
            Name = name
        };
    }

    public static AuditedProperty Mapped<TValue, TResult>(string name, Func<TValue, TResult> selector, string? displayName = null)
    {
        return new AuditedProperty
        {
            Name = name,
            DisplayName = displayName,
            ValueSelector = node =>
            {
                if (node is not JsonValue value || !value.TryGetValue<TValue>(out var raw))
                {
                    return null;
                }

                var result = selector(raw);
                return result is null ? null : JsonValue.Create(result);
            }
        };
    }

    public static AuditedProperty Related(string name, Func<EntityEntry, object?> snapshotSelector)
    {
        return new AuditedProperty
        {
            Name = name,
            SnapshotSelector = snapshotSelector
        };
    }
}
