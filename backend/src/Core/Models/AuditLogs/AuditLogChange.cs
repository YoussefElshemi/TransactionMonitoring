using System.Text.Json.Nodes;

namespace Core.Models.AuditLogs;

public record AuditLogChange
{
    public required string Field { get; init; }
    public required JsonNode? OldValue { get; init; }
    public required JsonNode? NewValue { get; init; }
}
