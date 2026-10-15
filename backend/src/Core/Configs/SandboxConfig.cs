namespace Core.Configs;

public record SandboxConfig
{
    public int DefaultMonthlyCredits { get; init; } = 50_000;
}
