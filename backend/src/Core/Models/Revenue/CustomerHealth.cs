using Core.Enums;
using Core.ValueObjects;

namespace Core.Models.Revenue;

public record CustomerHealth
{
    public required OrganizationId OrganizationId { get; init; }
    public required FriendlyName OrganizationName { get; init; }

    public required HealthScore HealthScore { get; init; }
    public required HealthBand HealthBand { get; init; }

    public required PeriodStart PeriodStart { get; init; }
    public required PeriodEnd PeriodEnd { get; init; }

    public required Count RecentUsage { get; init; }
    public required Count PreviousUsage { get; init; }

    public required TrendRate? UsageTrend { get; init; }

    public required ErrorRate RecentErrorRate { get; init; }
    public required ErrorRate PreviousErrorRate { get; init; }

    public required DaysElapsed? DaysSinceLastUsage { get; init; }

    public required DunningStage? ArrearsStage { get; init; }

    public required List<HealthSignal> Signals { get; init; }
}
