using Core.Enums;

namespace Core.Configs;

public record CaseConfig
{
    public Dictionary<RuleSeverity, int> DefaultSlaTargetMinutes { get; init; } = new()
    {
        [RuleSeverity.Info] = 10080,
        [RuleSeverity.Low] = 4320,
        [RuleSeverity.Medium] = 1440,
        [RuleSeverity.High] = 480,
        [RuleSeverity.Critical] = 120
    };

    public int FallbackSlaTargetMinutes { get; init; } = 10080;

    public int EscalationBatchSize { get; init; } = 100;
}
