namespace Core.Models.Webhooks;

public record WebhookMessage<T>
{
    public required string Event { get; init; }

    public required int Version { get; init; }

    public required DateTimeOffset Timestamp { get; init; }
    public required T Payload { get; init; }
}
