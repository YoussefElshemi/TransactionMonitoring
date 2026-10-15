namespace Core.Configs;

public record RuleConfig
{
    public int DefaultReviewThreshold { get; init; } = 100;
}
