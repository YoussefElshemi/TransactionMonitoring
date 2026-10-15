using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Core.Models.AuditLogs;
using Core.ValueObjects;

namespace Core.Helpers;

// Must stay identical to the database function the audit insert trigger calls.
public static class AuditChainHasher
{
    private const char FieldSeparator = '|';

    // Microseconds: Postgres rounds timestamps to that resolution, so hashing ticks would never verify.
    private const string TimestampFormat = "yyyy-MM-ddTHH:mm:ss.ffffffZ";

    public static Hash Compute(AuditLogEntry auditLogEntry)
    {
        var canonical = string.Join(FieldSeparator,
            auditLogEntry.AuditLogId.ToString(),
            ((int)auditLogEntry.EntityType).ToString(CultureInfo.InvariantCulture),
            auditLogEntry.EntityId.ToString(),
            auditLogEntry.OrganizationId?.ToString() ?? string.Empty,
            ((int)auditLogEntry.Operation).ToString(CultureInfo.InvariantCulture),
            auditLogEntry.Snapshot.ToString(),
            auditLogEntry.ChangedByUserId?.ToString() ?? string.Empty,
            auditLogEntry.ChangedByApiClientId?.ToString() ?? string.Empty,
            ((DateTimeOffset)auditLogEntry.CreatedAt).ToUniversalTime().ToString(TimestampFormat, CultureInfo.InvariantCulture),
            auditLogEntry.Sequence.ToString(),
            auditLogEntry.PreviousHash?.ToString() ?? string.Empty);

        return new Hash(Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant());
    }
}
