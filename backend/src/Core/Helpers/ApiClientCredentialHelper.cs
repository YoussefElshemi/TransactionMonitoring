using System.Security.Cryptography;
using System.Text;
using Core.Enums;
using Core.ValueObjects;

namespace Core.Helpers;

public static class ApiClientCredentialHelper
{
    private const string ClientIdPrefix = "tmc_";
    private const string ClientSecretPrefix = "tmsec_";

    private const string TestPrefix = "test_";
    private const int ClientIdByteLength = 16;
    private const int ClientSecretByteLength = 32;
    private const int DisplayedSecretCharacters = 6;

    public static ClientId GenerateClientId(Mode mode)
    {
        var bytes = RandomNumberGenerator.GetBytes(ClientIdByteLength);

        return new ClientId(Prefix(ClientIdPrefix, mode) + Convert.ToHexString(bytes).ToLowerInvariant());
    }

    public static ClientSecret GenerateClientSecret(Mode mode)
    {
        var bytes = RandomNumberGenerator.GetBytes(ClientSecretByteLength);

        return new ClientSecret(Prefix(ClientSecretPrefix, mode) + Convert.ToHexString(bytes).ToLowerInvariant());
    }

    public static SecretHash Hash(ClientSecret clientSecret)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(clientSecret));

        return new SecretHash(Convert.ToHexString(hash).ToLowerInvariant());
    }

    public static bool Verify(ClientSecret clientSecret, SecretHash secretHash)
    {
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(Hash(clientSecret)),
            Encoding.UTF8.GetBytes(secretHash));
    }

    public static SecretPrefix ToSecretPrefix(ClientSecret clientSecret)
    {
        string value = clientSecret;

        var prefixLength = value.StartsWith(ClientSecretPrefix + TestPrefix, StringComparison.Ordinal)
            ? ClientSecretPrefix.Length + TestPrefix.Length
            : ClientSecretPrefix.Length;

        return new SecretPrefix(value[..Math.Min(value.Length, prefixLength + DisplayedSecretCharacters)]);
    }

    private static string Prefix(string prefix, Mode mode)
    {
        return mode == Mode.Test ? prefix + TestPrefix : prefix;
    }
}
