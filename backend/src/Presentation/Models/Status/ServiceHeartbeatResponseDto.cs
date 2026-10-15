namespace Presentation.Models.Status;

public record ServiceHeartbeatResponseDto
{
    public required DateTimeOffset ObservedAt { get; init; }
    public required int ExpectedIntervalSeconds { get; init; }

    public required bool IsFresh { get; init; }

    public required List<ComponentHeartbeatResponseDto> Components { get; init; }
}

public record ComponentHeartbeatResponseDto
{
    public required string Component { get; init; }
    public required DateTimeOffset? LastProbedAt { get; init; }
    public required int? StaleForSeconds { get; init; }
    public required bool IsFresh { get; init; }
}
