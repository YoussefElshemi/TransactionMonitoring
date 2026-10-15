using System.Security.Cryptography;
using System.Text;
using Core.ValueObjects;

namespace Core.Helpers;

public static class PayloadCipher
{
    // Rows predating protection carry bare base64 and are read as-is, so no backfill is needed.
    public const string Version = "enc.v1.";

    private const int NonceByteCount = 12;
    private const int TagByteCount = 16;

    public static string Protect(DataKey dataKey, OrganizationId organizationId, string plaintext)
    {
        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var nonce = RandomNumberGenerator.GetBytes(NonceByteCount);
        var cipherText = new byte[plaintextBytes.Length];
        var tag = new byte[TagByteCount];

        using var aes = new AesGcm(dataKey, TagByteCount);
        aes.Encrypt(nonce, plaintextBytes, cipherText, tag, AssociatedData(organizationId));

        var envelope = new byte[NonceByteCount + TagByteCount + cipherText.Length];
        nonce.CopyTo(envelope, 0);
        tag.CopyTo(envelope, NonceByteCount);
        cipherText.CopyTo(envelope, NonceByteCount + TagByteCount);

        return Version + Convert.ToBase64String(envelope);
    }

    public static string Unprotect(DataKey dataKey, OrganizationId organizationId, string stored)
    {
        if (!IsProtected(stored))
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(stored));
        }

        var envelope = Convert.FromBase64String(stored[Version.Length..]);

        if (envelope.Length < NonceByteCount + TagByteCount)
        {
            throw new CryptographicException("The stored payload is too short to carry an envelope.");
        }

        var nonce = envelope.AsSpan(0, NonceByteCount);
        var tag = envelope.AsSpan(NonceByteCount, TagByteCount);
        var cipherText = envelope.AsSpan(NonceByteCount + TagByteCount);
        var plaintextBytes = new byte[cipherText.Length];

        using var aes = new AesGcm(dataKey, TagByteCount);
        aes.Decrypt(nonce, cipherText, tag, plaintextBytes, AssociatedData(organizationId));

        return Encoding.UTF8.GetString(plaintextBytes);
    }

    public static bool IsProtected(string stored)
    {
        return stored.StartsWith(Version, StringComparison.Ordinal);
    }

    // Organisation id is the associated data, so a row copied across tenants fails to decrypt.
    private static byte[] AssociatedData(OrganizationId organizationId)
    {
        return Encoding.UTF8.GetBytes(organizationId.ToString());
    }
}
