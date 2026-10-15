using System.Security.Cryptography;
using Core.ValueObjects;

namespace Core.Helpers;

public static class SecretKeyHelper
{
    private const string Prefix = "whsec_";
    private const int ByteLength = 32;

    public static SecretKey Generate()
    {
        var bytes = RandomNumberGenerator.GetBytes(ByteLength);

        return new SecretKey(Prefix + Convert.ToHexString(bytes).ToLowerInvariant());
    }
}
