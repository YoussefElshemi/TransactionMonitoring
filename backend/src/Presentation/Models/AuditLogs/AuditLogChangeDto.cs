using System.Text.Json.Nodes;

namespace Presentation.Models.AuditLogs;

public record AuditLogChangeDto
{
    public required string Field { get; init; }
    public required JsonNode? OldValue { get; init; }
    public required JsonNode? NewValue { get; init; }
}
