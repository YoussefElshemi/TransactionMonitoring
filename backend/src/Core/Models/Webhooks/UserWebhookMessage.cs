namespace Core.Models.Webhooks;

public record UserWebhookMessage
{
    public required Guid UserId { get; init; }
    public required string? Email { get; init; }
    public required string? UserName { get; init; }
}