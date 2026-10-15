using System.Security.Cryptography;
using System.Text;
using Core.ValueObjects;

namespace Core.Helpers;

public static class StatusSubscriptionTokenHelper
{
    private const int TokenByteLength = 32;

    public static StatusSubscriptionToken Generate()
    {
        var bytes = RandomNumberGenerator.GetBytes(TokenByteLength);

        return new StatusSubscriptionToken(Convert.ToHexString(bytes).ToLowerInvariant());
    }

    public static Hash Hash(StatusSubscriptionToken statusSubscriptionToken)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(statusSubscriptionToken));

        return new Hash(Convert.ToHexString(hash).ToLowerInvariant());
    }
}
