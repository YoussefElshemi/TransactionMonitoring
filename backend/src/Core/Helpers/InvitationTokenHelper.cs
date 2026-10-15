using System.Security.Cryptography;
using System.Text;
using Core.ValueObjects;

namespace Core.Helpers;

public static class InvitationTokenHelper
{
    private const int TokenByteLength = 32;

    public static InvitationToken Generate()
    {
        var bytes = RandomNumberGenerator.GetBytes(TokenByteLength);

        return new InvitationToken(Convert.ToHexString(bytes).ToLowerInvariant());
    }

    public static Hash Hash(InvitationToken invitationToken)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(invitationToken));

        return new Hash(Convert.ToHexString(hash).ToLowerInvariant());
    }
}
