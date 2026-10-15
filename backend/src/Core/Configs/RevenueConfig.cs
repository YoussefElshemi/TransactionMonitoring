using Core.Enums;

namespace Core.Configs;

public record RevenueConfig
{
    public Currency ReportingCurrency { get; init; } = Currency.Gbp;

    public decimal UsageDeclineThreshold { get; init; } = -20m;
    public decimal SevereUsageDeclineThreshold { get; init; } = -50m;

    public decimal ErrorRateRiseThreshold { get; init; } = 1m;

    public int SilenceDays { get; init; } = 14;

    public int HealthyScore { get; init; } = 70;
    public int WatchScore { get; init; } = 40;

    public int HealthWindowDays { get; init; } = 30;
}
