using Core.ValueObjects;

namespace Core.Models.AuditLogs;

public record AuditLogExport
{
    public required FileName FileName { get; init; }
    public required ContentType ContentType { get; init; }
    public required byte[] Content { get; init; }
}
