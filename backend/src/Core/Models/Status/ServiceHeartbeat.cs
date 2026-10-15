using Core.Enums;
using Core.ValueObjects;

namespace Core.Models.Status;

public record ServiceHeartbeat
{
    public required ObservedAt ObservedAt { get; init; }

    public required Count ExpectedIntervalSeconds { get; init; }

    public required List<ComponentHeartbeat> Components { get; init; }

    public required IsHealthy IsFresh { get; init; }
}

public record ComponentHeartbeat
{
    public required ServiceComponent Component { get; init; }

    public required ObservedAt? LastProbedAt { get; init; }

    public required Count? StaleForSeconds { get; init; }
    public required IsHealthy IsFresh { get; init; }
}
