namespace Core.Configs;

public record IdempotencyConfig
{
    public int RetentionHours { get; init; } = 24;
}
