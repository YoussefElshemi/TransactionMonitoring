using System.Text.Json.Nodes;
using Core.Models.AuditLogs;

namespace Core.Services;

public static class AuditLogChangeCalculator
{
    public static List<AuditLogChange> Compute(JsonObject? previous, JsonObject current)
    {
        var fields = new HashSet<string>(StringComparer.Ordinal);

        foreach (var property in current)
        {
            fields.Add(property.Key);
        }

        var changes = new List<AuditLogChange>();

        foreach (var field in fields)
        {
            var oldValue = previous?[field];
            var newValue = current[field];

            if (AreEqual(oldValue, newValue))
            {
                continue;
            }

            changes.Add(new AuditLogChange
            {
                Field = field,
                OldValue = Detach(oldValue),
                NewValue = Detach(newValue)
            });
        }

        return changes;
    }

    private static bool AreEqual(JsonNode? left, JsonNode? right)
    {
        if (left is null || right is null)
        {
            return left is null && right is null;
        }

        return string.Equals(left.ToJsonString(), right.ToJsonString(), StringComparison.Ordinal);
    }

    private static JsonNode? Detach(JsonNode? node)
    {
        return node is null ? null : JsonNode.Parse(node.ToJsonString());
    }
}
