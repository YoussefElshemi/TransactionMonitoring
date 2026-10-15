using System.Security.Cryptography;
using Core.Configs;
using Core.ValueObjects;

namespace Core.Helpers;

public static class DataKeyWrapper
{
    private const int NonceByteCount = 12;
    private const int TagByteCount = 16;

    public static DataKey Create()
    {
        return new DataKey(RandomNumberGenerator.GetBytes(DataKey.ByteCount));
    }

    public static string Wrap(byte[] masterKey, OrganizationId organizationId, DataKey dataKey)
    {
        return PayloadCipher.Protect(new DataKey(masterKey), organizationId, Convert.ToBase64String(dataKey));
    }

    public static DataKey Unwrap(byte[] masterKey, OrganizationId organizationId, string wrapped)
    {
        var unwrapped = PayloadCipher.Unprotect(new DataKey(masterKey), organizationId, wrapped);

        return new DataKey(Convert.FromBase64String(unwrapped));
    }

    public static byte[] ReadMasterKey(string configured)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(configured);

        byte[] masterKey;

        try
        {
            masterKey = Convert.FromBase64String(configured);
        }
        catch (FormatException exception)
        {
            throw new InvalidOperationException("The configured payload protection master key is not valid base64.", exception);
        }

        if (masterKey.Length != DataKey.ByteCount)
        {
            throw new InvalidOperationException(
                $"The configured payload protection master key is {masterKey.Length} bytes; it must be {DataKey.ByteCount}.");
        }

        return masterKey;
    }

    public static bool IsDevelopmentMasterKey(string configured)
    {
        return string.Equals(configured, PayloadProtectionConfig.DevelopmentMasterKey, StringComparison.Ordinal);
    }
}
