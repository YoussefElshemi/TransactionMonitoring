using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Core.Models.AuditLogs;
using Core.ValueObjects;

namespace Core.Helpers;

public static class AuditAnchorSigner
{
    private const int KeyByteCount = 32;

    private static readonly byte[] DerivationInfo = "transaction-monitoring/audit-anchor/v1"u8.ToArray();

    public static Signature Sign(
        byte[] masterKey,
        AuditAnchorId auditAnchorId,
        OrganizationId? organizationId,
        Sequence sequence,
        Hash hash,
        CreatedAt createdAt)
    {
        var canonical = string.Join('|',
            auditAnchorId.ToString(),
            organizationId?.ToString() ?? string.Empty,
            sequence.ToString(),
            hash.ToString(),
            ((DateTimeOffset)createdAt).ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));

        var signature = HMACSHA256.HashData(DeriveKey(masterKey), Encoding.UTF8.GetBytes(canonical));

        return new Signature(Convert.ToHexString(signature).ToLowerInvariant());
    }

    // Fixed-time comparison to avoid a timing side channel.
    public static bool Verify(byte[] masterKey, AuditAnchor auditAnchor)
    {
        var expected = Sign(
            masterKey,
            auditAnchor.AuditAnchorId,
            auditAnchor.OrganizationId,
            auditAnchor.Sequence,
            auditAnchor.Hash,
            auditAnchor.CreatedAt);

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(auditAnchor.Signature));
    }

    private static byte[] DeriveKey(byte[] masterKey)
    {
        return HKDF.DeriveKey(HashAlgorithmName.SHA256, masterKey, KeyByteCount, info: DerivationInfo);
    }
}
