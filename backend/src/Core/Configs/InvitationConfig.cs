namespace Core.Configs;

public record InvitationConfig
{
    public int ExpiryDays { get; init; } = 14;

    public string AcceptUrl { get; init; } = "http://localhost:3000/invitations/accept";
}
