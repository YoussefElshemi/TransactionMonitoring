namespace Core.Configs;

public record BackgroundServiceConfig
{
    public bool OutboxEnabled { get; init; }
    public int OutboxIntervalInMs { get; init; }
    public int OutboxMaxAttempts { get; init; }
    public int BackTestChunkSize { get; init; }
    public int VelocityBackfillChunkSize { get; init; } = 200;

    public int SimulationChunkSize { get; init; } = 25;

    public int MaxConcurrency { get; init; } = 10;

    public int MaxBatchSize { get; init; } = 5;

    public int ClusterCheckinIntervalSeconds { get; init; } = 20;
}