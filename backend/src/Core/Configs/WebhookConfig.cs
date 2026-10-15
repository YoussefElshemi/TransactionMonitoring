namespace Core.Configs;

public record WebhookConfig
{
    public int AutoDisableAfterConsecutiveFailures { get; init; } = 20;

    public int ReplayRangeLimit { get; init; } = 500;

    public int HealthWindowDays { get; init; } = 7;

    public int ResponseBodyLimit { get; init; } = 512;

    public string[] SourceIpRanges { get; init; } = [];
}
