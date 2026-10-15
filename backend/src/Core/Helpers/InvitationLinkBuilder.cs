using Core.ValueObjects;

namespace Core.Helpers;

public static class InvitationLinkBuilder
{
    private const string TokenParameter = "token";

    public static string Build(string acceptUrl, InvitationToken invitationToken)
    {
        var separator = acceptUrl.Contains('?', StringComparison.Ordinal) ? '&' : '?';

        return $"{acceptUrl}{separator}{TokenParameter}={Uri.EscapeDataString(invitationToken)}";
    }
}
