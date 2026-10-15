using System.Globalization;
using System.Text;
using Core.Models.AuditLogs;

namespace Core.Documents;

public static class AuditLogCsvDocument
{
    public const string ContentType = "text/csv";

    private const string LineSeparator = "\r\n";
    private const string TimestampFormat = "yyyy-MM-ddTHH:mm:ss.ffffffZ";

    private static readonly string[] Headers =
    [
        "Sequence",
        "AuditLogId",
        "CreatedAt",
        "EntityType",
        "EntityId",
        "OrganizationId",
        "Operation",
        "ChangedByUserId",
        "ChangedByApiClientId",
        "Snapshot",
        "PreviousHash",
        "Hash"
    ];

    public static byte[] Write(IReadOnlyCollection<AuditLogEntry> auditLogEntries)
    {
        var builder = new StringBuilder();

        builder.Append(string.Join(',', Headers)).Append(LineSeparator);

        foreach (var auditLogEntry in auditLogEntries)
        {
            builder.Append(string.Join(',',
                Quote(auditLogEntry.Sequence.ToString()),
                Quote(auditLogEntry.AuditLogId.ToString()),
                Quote(((DateTimeOffset)auditLogEntry.CreatedAt).ToUniversalTime().ToString(TimestampFormat, CultureInfo.InvariantCulture)),
                Quote(auditLogEntry.EntityType.ToString()),
                Quote(auditLogEntry.EntityId.ToString()),
                Quote(auditLogEntry.OrganizationId?.ToString() ?? string.Empty),
                Quote(auditLogEntry.Operation.ToString()),
                Quote(auditLogEntry.ChangedByUserId?.ToString() ?? string.Empty),
                Quote(auditLogEntry.ChangedByApiClientId?.ToString() ?? string.Empty),
                Quote(auditLogEntry.Snapshot.ToString()),
                Quote(auditLogEntry.PreviousHash?.ToString() ?? string.Empty),
                Quote(auditLogEntry.Hash.ToString()))).Append(LineSeparator);
        }

        // GetBytes never emits a BOM; without one spreadsheets guess the code page and mangle non-ASCII names.
        var encoding = new UTF8Encoding(true);

        return [.. encoding.GetPreamble(), .. encoding.GetBytes(builder.ToString())];
    }

    private static string Quote(string value)
    {
        return $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
    }
}
