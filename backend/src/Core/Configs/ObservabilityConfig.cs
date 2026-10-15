namespace Core.Configs;

public record ObservabilityConfig
{
    public string OtelCollectorEndpoint { get; init; } = null!;
    public bool OutputLogsToOtel { get; init; }
    public bool OutputLogsToConsole { get; init; }
}