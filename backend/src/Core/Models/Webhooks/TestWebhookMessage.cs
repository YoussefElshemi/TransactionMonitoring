namespace Core.Models.Webhooks;

public record TestWebhookMessage
{
    public required Guid TestId { get; init; }
}