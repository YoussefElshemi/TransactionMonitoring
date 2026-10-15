using Core.ValueObjects;

namespace Core.Models.OutboxMessages;

public record UpdateOutboxMessageRequest
{
    public required OutboxMessageId OutboxMessageId { get; init; }
    public required Attempts Attempts { get; init; }

    public required LastAttemptedAt? LastAttemptedAt { get; init; }
    public required StatusCode? ResponseStatusCode { get; init; }
    public required ResponseBody? ResponseBody { get; init; }
    public required LatencyMilliseconds? Latency { get; init; }
}
