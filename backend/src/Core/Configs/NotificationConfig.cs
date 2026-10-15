namespace Core.Configs;

public record NotificationConfig
{
    public int BreachBatchSize { get; init; } = 100;
    public int OverdueFilingBatchSize { get; init; } = 100;
}
